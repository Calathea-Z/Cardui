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
        RejectToken(input);
        RejectToken(request.Instructions);
        using var response = await SendAsync(input, request.Instructions, cancellationToken);
        var text = await ReadTextAsync(response, cancellationToken);
        return new PlanningModelReply(text);
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
    private static void RejectToken(string? text)
    {
        if (PlanningShareRules.ContainsToken(text))
        {
            throw new BadRequestException(PlanningShareRules.TokenRefusal);
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
    /// Posts one Responses request. The provider is told not to store it.
    /// </summary>
    private async Task<HttpResponseMessage> SendAsync(
        string input,
        string? instructions,
        CancellationToken cancellationToken)
    {
        try
        {
            using var message = CreateMessage(input, instructions);
            var response = await _httpClient.SendAsync(message, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                using (response)
                {
                    await ThrowForStatusAsync(response, cancellationToken);
                }
            }

            return response;
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
                "Planning model request failed. {ErrorType}",
                exception.GetType().Name);
            throw new PlanningModelException(
                "The planning model could not answer. Please try again.",
                exception);
        }
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
            ["store"] = false
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
    /// Logs the status and provider error code, then throws a message without the body.
    /// </summary>
    private async Task ThrowForStatusAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var (type, code) = await ReadErrorCodesAsync(response, cancellationToken);
        _logger.LogError(
            "Planning model request failed with status {StatusCode}. {ErrorType}/{ErrorCode}",
            (int)response.StatusCode,
            type ?? "none",
            code ?? "none");
        throw new PlanningModelException(
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
    /// </summary>
    private async Task<string> ReadTextAsync(
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

            return text;
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
                "The planning model could not answer. Please try again.",
                exception);
        }
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
