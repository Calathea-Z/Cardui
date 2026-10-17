using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cardui.Api.Configuration;
using Cardui.Api.Domain.Planning;
using Cardui.Api.Dtos.Planning;
using Cardui.Api.Exceptions;
using Cardui.Api.Options;
using Cardui.Api.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace Cardui.Api.Services.Planning;

public sealed class OpenAiPlanningModel : IPlanningModel
{
    private readonly HttpClient _httpClient;
    private readonly PlanningModelOptions _options;
    private readonly ILogger<OpenAiPlanningModel> _logger;

    public OpenAiPlanningModel(
        HttpClient httpClient,
        IOptions<PlanningModelOptions> options,
        ILogger<OpenAiPlanningModel> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
        _httpClient.Timeout = PlanningModelConfiguration.RequestTimeout;
    }

    /// <inheritdoc />
    public async Task<PlanningModelReply> ReplyAsync(
        PlanningModelRequest request,
        CancellationToken cancellationToken)
    {
        RequireConfigured();
        var input = RequireInput(request);
        RejectSecrets(input, request.Instructions);
        RejectLength(input, request.Instructions);
        return await SendWithLimitsAsync(input, request.Instructions, cancellationToken);
    }

    #region Private Methods

    /// <summary>
    /// Stops the call when the key or model id is missing.
    /// </summary>
    private void RequireConfigured()
    {
        if (!PlanningModelConfiguration.IsConfigured(_options))
        {
            throw new PlanningModelNotConfiguredException();
        }
    }

    /// <summary>
    /// Refuses a bank access token before any request is posted.
    /// The token is not logged.
    /// </summary>
    private static void RejectSecrets(string input, string? instructions)
    {
        var error = PlanningCallRules.RejectToken(input, instructions);
        if (error is not null)
        {
            throw new BadRequestException(error);
        }
    }

    /// <summary>
    /// Refuses text past the character cap before any request is posted.
    /// </summary>
    private static void RejectLength(string input, string? instructions)
    {
        var error = PlanningCallRules.RejectLength(input, instructions);
        if (error is not null)
        {
            throw new BadRequestException(error);
        }
    }

    /// <summary>
    /// Requires a message. Blank text is not sent to the provider.
    /// </summary>
    private static string RequireInput(PlanningModelRequest request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Input))
        {
            throw new BadRequestException("Enter a message for the planning model.");
        }

        return request.Input.Trim();
    }

    /// <summary>
    /// Posts until the provider answers or the attempt limit is reached.
    /// A transient failure waits, then tries again. The caller cancelling stops the call.
    /// </summary>
    private async Task<PlanningModelReply> SendWithLimitsAsync(
        string input,
        string? instructions,
        CancellationToken cancellationToken)
    {
        PlanningCallRetryException? failure = null;
        for (var attempt = 1; attempt <= PlanningCallRules.MaxAttempts; attempt++)
        {
            try
            {
                return await SendAttemptAsync(input, instructions, attempt, cancellationToken);
            }
            catch (PlanningCallRetryException retry)
            {
                failure = retry;
                if (!PlanningCallRules.CanRetry(attempt))
                {
                    break;
                }

                _logger.LogWarning(
                    "{Detail}",
                    PlanningCallRules.DescribeRetry(
                        attempt,
                        retry.StatusCode,
                        retry.ErrorType,
                        retry.ErrorCode,
                        retry.ExceptionType));
                await Task.Delay(PlanningCallRules.RetryDelay, cancellationToken);
            }
        }

        _logger.LogError(
            "{Detail}",
            PlanningCallRules.DescribeFailure(
                PlanningCallRules.MaxAttempts,
                failure?.StatusCode,
                failure?.ErrorType,
                failure?.ErrorCode,
                failure?.ExceptionType));
        throw new PlanningModelException(
            "The planning model could not answer. Please try again.");
    }

    /// <summary>
    /// Posts one attempt. A transient HTTP or transport failure can be tried again.
    /// A completed response is not posted again.
    /// </summary>
    private async Task<PlanningModelReply> SendAttemptAsync(
        string input,
        string? instructions,
        int attempt,
        CancellationToken cancellationToken)
    {
        try
        {
            using var message = CreateMessage(input, instructions);
            using var response = await _httpClient.SendAsync(message, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw await FailStatusAsync(response, attempt, cancellationToken);
            }

            var (text, inputTokens, outputTokens) = await ReadTextAsync(response, cancellationToken);
            _logger.LogInformation(
                "{Detail}",
                PlanningCallRules.DescribeAnswer(attempt, inputTokens, outputTokens));
            return new PlanningModelReply(text);
        }
        catch (PlanningModelException)
        {
            throw;
        }
        catch (PlanningCallRetryException)
        {
            throw;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException exception)
        {
            throw Retry(exception);
        }
        catch (HttpRequestException exception)
        {
            throw Retry(exception);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                "{Detail}",
                PlanningCallRules.DescribeFailure(
                    attempt,
                    null,
                    null,
                    null,
                    exception.GetType().Name));
            throw new PlanningModelException(
                "The planning model could not answer. Please try again.");
        }
    }

    /// <summary>
    /// Marks a transport failure as retryable. The exception text is not kept.
    /// </summary>
    private static PlanningCallRetryException Retry(Exception exception)
    {
        return new PlanningCallRetryException(null, null, null, exception.GetType().Name);
    }

    /// <summary>
    /// Builds the Responses request. The API key stays in the header.
    /// </summary>
    private HttpRequestMessage CreateMessage(string input, string? instructions)
    {
        var payload = new JsonObject
        {
            ["model"] = _options.Model.Trim(),
            ["input"] = input,
            ["store"] = false,
            ["max_output_tokens"] = PlanningCallRules.MaxOutputTokens
        };

        if (!string.IsNullOrWhiteSpace(instructions))
        {
            payload["instructions"] = instructions.Trim();
        }

        var message = new HttpRequestMessage(HttpMethod.Post, "responses")
        {
            Content = new StringContent(
                payload.ToJsonString(),
                Encoding.UTF8,
                "application/json")
        };
        message.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            _options.ApiKey.Trim());
        return message;
    }

    /// <summary>
    /// Reads the provider error type and code.
    /// A transient status can be tried again. Any other status fails once, without the response body.
    /// </summary>
    private async Task<Exception> FailStatusAsync(
        HttpResponseMessage response,
        int attempt,
        CancellationToken cancellationToken)
    {
        var (type, code) = await ReadErrorCodesAsync(response, cancellationToken);
        var status = (int)response.StatusCode;
        if (PlanningCallRules.IsTransient(status))
        {
            return new PlanningCallRetryException(status, type, code, null);
        }

        _logger.LogError(
            "{Detail}",
            PlanningCallRules.DescribeFailure(attempt, status, type, code, null));
        return new PlanningModelException(
            "The planning model could not answer. Please try again.");
    }

    /// <summary>
    /// Reads the provider error type and code. The error text is left unread.
    /// </summary>
    private static async Task<(string? Type, string? Code)> ReadErrorCodesAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(
                stream,
                cancellationToken: cancellationToken);
            if (!document.RootElement.TryGetProperty("error", out var error)
                || error.ValueKind != JsonValueKind.Object)
            {
                return (null, null);
            }

            return (ReadString(error, "type"), ReadString(error, "code"));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return (null, null);
        }
    }

    /// <summary>
    /// Reads assistant text from output_text, or from message content when that field is absent.
    /// Token counts come from usage. Any other usage field is ignored.
    /// </summary>
    private async Task<(string Text, int? InputTokens, int? OutputTokens)> ReadTextAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(
                stream,
                cancellationToken: cancellationToken);
            var text = ReadOutputText(document.RootElement);
            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogError("Planning model response had no text.");
                throw new PlanningModelException(
                    "The planning model could not answer. Please try again.");
            }

            return (
                text,
                ReadTokenCount(document.RootElement, "input_tokens"),
                ReadTokenCount(document.RootElement, "output_tokens"));
        }
        catch (PlanningModelException)
        {
            throw;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                "Planning model response could not be read. {ErrorType}",
                exception.GetType().Name);
            throw new PlanningModelException(
                "The planning model could not answer. Please try again.");
        }
    }

    /// <summary>
    /// Reads one token count from usage. A missing, fractional, or negative value is left unknown.
    /// Other usage fields are ignored.
    /// </summary>
    private static int? ReadTokenCount(JsonElement root, string name)
    {
        if (!root.TryGetProperty("usage", out var usage)
            || usage.ValueKind != JsonValueKind.Object
            || !usage.TryGetProperty(name, out var value)
            || value.ValueKind != JsonValueKind.Number
            || !value.TryGetInt32(out var count)
            || count < 0)
        {
            return null;
        }

        return count;
    }

    /// <summary>
    /// Joins the provider's text parts. A function call or empty message is not text.
    /// </summary>
    private static string? ReadOutputText(JsonElement root)
    {
        if (root.TryGetProperty("output_text", out var outputText)
            && outputText.ValueKind == JsonValueKind.String)
        {
            var text = outputText.GetString();
            if (!string.IsNullOrWhiteSpace(text))
            {
                return text;
            }
        }

        if (!root.TryGetProperty("output", out var output)
            || output.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var parts = new StringBuilder();
        foreach (var item in output.EnumerateArray())
        {
            AppendOutputText(item, parts);
        }

        return parts.Length == 0 ? null : parts.ToString();
    }

    /// <summary>
    /// Appends output_text parts from one response item.
    /// </summary>
    private static void AppendOutputText(JsonElement item, StringBuilder parts)
    {
        if (!item.TryGetProperty("content", out var content)
            || content.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var part in content.EnumerateArray())
        {
            if (!part.TryGetProperty("type", out var type)
                || type.GetString() != "output_text"
                || !part.TryGetProperty("text", out var textElement)
                || textElement.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            var text = textElement.GetString();
            if (!string.IsNullOrEmpty(text))
            {
                parts.Append(text);
            }
        }
    }

    /// <summary>
    /// Reads one string property. A missing or non-string value is null.
    /// </summary>
    private static string? ReadString(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value)
            || value.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        var text = value.GetString();
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    #endregion
}
