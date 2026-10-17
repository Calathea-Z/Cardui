using System.Net;
using System.Text;
using System.Text.Json;
using Cardui.Api.Configuration;
using Cardui.Api.Domain.Planning;
using Cardui.Api.Dtos.Planning;
using Cardui.Api.Exceptions;
using Cardui.Api.Options;
using Cardui.Api.Services.Planning;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Cardui.Tests.Services;

public class OpenAiPlanningModelTests
{
    private const string ApiKey = "sk-cardui-test-key";

    [Fact]
    public void Constructor_BoundsAHungCall()
    {
        var handler = new RecordingHandler();
        using var client = new HttpClient(handler)
        {
            BaseAddress = PlanningModelConfiguration.Endpoint
        };
        _ = new OpenAiPlanningModel(
            client,
            Microsoft.Extensions.Options.Options.Create(ConfiguredOptions()),
            new ListLogger());

        Assert.Equal(PlanningModelConfiguration.RequestTimeout, client.Timeout);
    }

    [Fact]
    public async Task ReplyAsync_DoesNotCallOpenAiWhenTheModelIsNotConfigured()
    {
        var handler = new RecordingHandler();
        var model = CreateModel(handler, new PlanningModelOptions());

        var exception = await Assert.ThrowsAsync<PlanningModelNotConfiguredException>(() =>
            model.ReplyAsync(new PlanningModelRequest("Hello", null), CancellationToken.None));

        Assert.Equal("Guided conversation is not configured.", exception.Message);
        Assert.Null(handler.Request);
    }

    [Fact]
    public async Task ReplyAsync_RejectsABlankMessageBeforeCallingOpenAi()
    {
        var handler = new RecordingHandler();
        var model = CreateModel(handler, ConfiguredOptions());

        var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
            model.ReplyAsync(new PlanningModelRequest("  ", "Stay brief."), CancellationToken.None));

        Assert.Equal("Enter a message for the planning model.", exception.Message);
        Assert.Null(handler.Request);
    }

    [Fact]
    public async Task ReplyAsync_SendsTheConfiguredModelAndDoesNotStoreTheCall()
    {
        var handler = new RecordingHandler();
        var model = CreateModel(handler, ConfiguredOptions());

        var reply = await model.ReplyAsync(
            new PlanningModelRequest("  What is next?  ", "  Stay brief.  "),
            CancellationToken.None);

        Assert.Equal("Keep the minimum.", reply.Text);
        Assert.NotNull(handler.Request);
        Assert.Equal(HttpMethod.Post, handler.Request.Method);
        Assert.Equal(
            new Uri(PlanningModelConfiguration.Endpoint, "responses"),
            handler.Request.RequestUri);
        Assert.Equal("Bearer", handler.Request.Headers.Authorization?.Scheme);
        Assert.Equal(ApiKey, handler.Request.Headers.Authorization?.Parameter);

        using var body = JsonDocument.Parse(handler.Body);
        Assert.Equal("configured-model", body.RootElement.GetProperty("model").GetString());
        Assert.Equal("What is next?", body.RootElement.GetProperty("input").GetString());
        Assert.Equal("Stay brief.", body.RootElement.GetProperty("instructions").GetString());
        Assert.False(body.RootElement.GetProperty("store").GetBoolean());
        Assert.Equal(PlanningCallRules.MaxOutputTokens, body.RootElement.GetProperty("max_output_tokens").GetInt32());
        Assert.False(body.RootElement.TryGetProperty("tools", out _));
        Assert.DoesNotContain(ApiKey, handler.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReplyAsync_OmitsDeveloperTextWhenInstructionsAreBlank()
    {
        var handler = new RecordingHandler();
        var model = CreateModel(handler, ConfiguredOptions());

        await model.ReplyAsync(new PlanningModelRequest("Hello", "  "), CancellationToken.None);

        using var body = JsonDocument.Parse(handler.Body);
        Assert.False(body.RootElement.TryGetProperty("instructions", out _));
    }

    [Fact]
    public async Task ReplyAsync_ReadsTextFromMessageContent()
    {
        var handler = new RecordingHandler
        {
            ResponseBody = """
                {
                  "output": [
                    {
                      "type": "message",
                      "content": [
                        { "type": "output_text", "text": "Keep " },
                        { "type": "output_text", "text": "the minimum." }
                      ]
                    }
                  ]
                }
                """
        };
        var model = CreateModel(handler, ConfiguredOptions());

        var reply = await model.ReplyAsync(
            new PlanningModelRequest("Hello", null),
            CancellationToken.None);

        Assert.Equal("Keep the minimum.", reply.Text);
    }

    [Fact]
    public async Task ReplyAsync_HidesTheProviderErrorBody()
    {
        var handler = new RecordingHandler
        {
            StatusCode = HttpStatusCode.Unauthorized,
            ResponseBody = $$"""
                {
                  "error": {
                    "message": "rejected {{ApiKey}} and salary 99999",
                    "type": "invalid_request_error",
                    "code": "invalid_api_key"
                  }
                }
                """
        };
        var logger = new ListLogger();
        var model = CreateModel(handler, ConfiguredOptions(), logger);

        var exception = await Assert.ThrowsAsync<PlanningModelException>(() =>
            model.ReplyAsync(new PlanningModelRequest("Hello", null), CancellationToken.None));

        Assert.Equal("The planning model could not answer. Please try again.", exception.Message);
        Assert.Equal(1, handler.Calls);
        var logged = string.Join(" ", logger.Messages);
        Assert.Contains("invalid_request_error", logged, StringComparison.Ordinal);
        Assert.Contains("invalid_api_key", logged, StringComparison.Ordinal);
        Assert.DoesNotContain(ApiKey, logged, StringComparison.Ordinal);
        Assert.DoesNotContain("99999", logged, StringComparison.Ordinal);
        Assert.DoesNotContain(ApiKey, exception.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReplyAsync_FailsWhenTheResponseHasNoText()
    {
        var handler = new RecordingHandler
        {
            ResponseBody = """{"output":[{"type":"function_call"}]}"""
        };
        var model = CreateModel(handler, ConfiguredOptions());

        var exception = await Assert.ThrowsAsync<PlanningModelException>(() =>
            model.ReplyAsync(new PlanningModelRequest("Hello", null), CancellationToken.None));

        Assert.Equal("The planning model could not answer. Please try again.", exception.Message);
        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task ReplyAsync_RetriesATransientFailureThenReadsTheText()
    {
        var handler = new RecordingHandler();
        handler.Script.Add(new ScriptedCall(
            HttpStatusCode.ServiceUnavailable,
            """
            {
              "error": {
                "message": "busy with salary 99999",
                "type": "server_error",
                "code": "overloaded"
              }
            }
            """));
        handler.Script.Add(new ScriptedCall(
            HttpStatusCode.OK,
            """{"output_text":"Keep the minimum.","usage":{"input_tokens":11,"output_tokens":4}}"""));
        var logger = new ListLogger();
        var model = CreateModel(handler, ConfiguredOptions(), logger);

        var reply = await model.ReplyAsync(
            new PlanningModelRequest("What is next?", "Stay brief."),
            CancellationToken.None);

        Assert.Equal("Keep the minimum.", reply.Text);
        Assert.Equal(2, handler.Calls);
        Assert.Equal(2, handler.Bodies.Count);
        Assert.All(handler.Bodies, body =>
        {
            using var document = JsonDocument.Parse(body);
            Assert.Equal(
                PlanningCallRules.MaxOutputTokens,
                document.RootElement.GetProperty("max_output_tokens").GetInt32());
            Assert.False(document.RootElement.GetProperty("store").GetBoolean());
            Assert.DoesNotContain(ApiKey, body, StringComparison.Ordinal);
        });
        var logged = string.Join(" ", logger.Messages);
        Assert.Contains("will retry after attempt 1", logged, StringComparison.Ordinal);
        Assert.Contains("503", logged, StringComparison.Ordinal);
        Assert.Contains("server_error", logged, StringComparison.Ordinal);
        Assert.Contains("overloaded", logged, StringComparison.Ordinal);
        Assert.Contains("Input tokens 11", logged, StringComparison.Ordinal);
        Assert.Contains("Output tokens 4", logged, StringComparison.Ordinal);
        Assert.DoesNotContain("Keep the minimum.", logged, StringComparison.Ordinal);
        Assert.DoesNotContain(ApiKey, logged, StringComparison.Ordinal);
        Assert.DoesNotContain("99999", logged, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReplyAsync_StopsAfterThreeTransientFailures()
    {
        var handler = new RecordingHandler
        {
            StatusCode = HttpStatusCode.TooManyRequests,
            ResponseBody = """
                {
                  "error": {
                    "message": "slow down salary 99999",
                    "type": "rate_limit_exceeded",
                    "code": "rate_limit_exceeded"
                  }
                }
                """
        };
        var logger = new ListLogger();
        var model = CreateModel(handler, ConfiguredOptions(), logger);

        var exception = await Assert.ThrowsAsync<PlanningModelException>(() =>
            model.ReplyAsync(new PlanningModelRequest("Hello", null), CancellationToken.None));

        Assert.Equal("The planning model could not answer. Please try again.", exception.Message);
        Assert.Equal(PlanningCallRules.MaxAttempts, handler.Calls);
        var logged = string.Join(" ", logger.Messages);
        Assert.Contains("will retry after attempt 1", logged, StringComparison.Ordinal);
        Assert.Contains("will retry after attempt 2", logged, StringComparison.Ordinal);
        Assert.Contains("failed on attempt 3", logged, StringComparison.Ordinal);
        Assert.Contains("429", logged, StringComparison.Ordinal);
        Assert.DoesNotContain(ApiKey, logged, StringComparison.Ordinal);
        Assert.DoesNotContain("99999", logged, StringComparison.Ordinal);
        Assert.DoesNotContain("slow down", logged, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReplyAsync_RetriesATimeoutTheCallerDidNotCancel()
    {
        var handler = new RecordingHandler();
        handler.Script.Add(new ScriptedCall(
            HttpStatusCode.OK,
            """{"output_text":"Keep the minimum."}""",
            new TaskCanceledException()));
        handler.Script.Add(new ScriptedCall(
            HttpStatusCode.OK,
            """{"output_text":"Keep the minimum."}"""));
        var logger = new ListLogger();
        var model = CreateModel(handler, ConfiguredOptions(), logger);

        var reply = await model.ReplyAsync(
            new PlanningModelRequest("Hello", null),
            CancellationToken.None);

        Assert.Equal("Keep the minimum.", reply.Text);
        Assert.Equal(2, handler.Calls);
        var logged = string.Join(" ", logger.Messages);
        Assert.Contains(nameof(TaskCanceledException), logged, StringComparison.Ordinal);
        Assert.Contains("Usage was not reported.", logged, StringComparison.Ordinal);
        Assert.DoesNotContain("Keep the minimum.", logged, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReplyAsync_RetriesANetworkFailureThenReadsTheText()
    {
        var handler = new RecordingHandler();
        handler.Script.Add(new ScriptedCall(
            HttpStatusCode.OK,
            """{"output_text":"Keep the minimum."}""",
            new HttpRequestException("refused " + ApiKey)));
        handler.Script.Add(new ScriptedCall(
            HttpStatusCode.OK,
            """{"output_text":"Keep the minimum."}"""));
        var logger = new ListLogger();
        var model = CreateModel(handler, ConfiguredOptions(), logger);

        var reply = await model.ReplyAsync(
            new PlanningModelRequest("Hello", null),
            CancellationToken.None);

        Assert.Equal("Keep the minimum.", reply.Text);
        Assert.Equal(2, handler.Calls);
        var logged = string.Join(" ", logger.Messages);
        Assert.Contains(nameof(HttpRequestException), logged, StringComparison.Ordinal);
        Assert.DoesNotContain(ApiKey, logged, StringComparison.Ordinal);
        Assert.DoesNotContain("Keep the minimum.", logged, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReplyAsync_DoesNotRetryAnUnexpectedFailure()
    {
        var handler = new RecordingHandler();
        handler.Script.Add(new ScriptedCall(
            HttpStatusCode.OK,
            """{"output_text":"Keep the minimum."}""",
            new InvalidOperationException("leaked " + ApiKey)));
        var logger = new ListLogger();
        var model = CreateModel(handler, ConfiguredOptions(), logger);

        var exception = await Assert.ThrowsAsync<PlanningModelException>(() =>
            model.ReplyAsync(new PlanningModelRequest("Hello", null), CancellationToken.None));

        Assert.Equal("The planning model could not answer. Please try again.", exception.Message);
        Assert.Equal(1, handler.Calls);
        var logged = string.Join(" ", logger.Messages);
        Assert.Contains(nameof(InvalidOperationException), logged, StringComparison.Ordinal);
        Assert.DoesNotContain(ApiKey, logged, StringComparison.Ordinal);
        Assert.DoesNotContain(ApiKey, exception.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReplyAsync_DoesNotSendTextPastTheCharacterCap()
    {
        var handler = new RecordingHandler();
        var model = CreateModel(handler, ConfiguredOptions());
        var tooLong = new string('a', PlanningCallRules.MaxInputCharacters + 1);

        var input = await Assert.ThrowsAsync<BadRequestException>(() =>
            model.ReplyAsync(new PlanningModelRequest(tooLong, "Stay brief."), CancellationToken.None));
        var instructions = await Assert.ThrowsAsync<BadRequestException>(() =>
            model.ReplyAsync(
                new PlanningModelRequest("Hello", tooLong),
                CancellationToken.None));

        Assert.Equal(PlanningCallRules.InputTooLong, input.Message);
        Assert.Equal(PlanningCallRules.InputTooLong, instructions.Message);
        Assert.Equal(0, handler.Calls);
    }

    [Fact]
    public async Task ReplyAsync_DoesNotLogAnUnreadableBody()
    {
        var handler = new RecordingHandler
        {
            ResponseBody = ApiKey + " salary 99999"
        };
        var logger = new ListLogger();
        var model = CreateModel(handler, ConfiguredOptions(), logger);

        var exception = await Assert.ThrowsAsync<PlanningModelException>(() =>
            model.ReplyAsync(new PlanningModelRequest("Hello", null), CancellationToken.None));

        Assert.Equal("The planning model could not answer. Please try again.", exception.Message);
        Assert.Equal(1, handler.Calls);
        var logged = string.Join(" ", logger.Messages);
        Assert.Contains("could not be read", logged, StringComparison.Ordinal);
        Assert.DoesNotContain(ApiKey, logged, StringComparison.Ordinal);
        Assert.DoesNotContain("99999", logged, StringComparison.Ordinal);
        Assert.DoesNotContain(ApiKey, exception.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReplyAsync_RedactsAnUnsafeErrorCode()
    {
        var handler = new RecordingHandler
        {
            StatusCode = HttpStatusCode.BadRequest,
            ResponseBody = $$"""
                {
                  "error": {
                    "message": "rejected {{ApiKey}}",
                    "type": "salary 99999",
                    "code": "dp:v1:protected-token"
                  }
                }
                """
        };
        var logger = new ListLogger();
        var model = CreateModel(handler, ConfiguredOptions(), logger);

        await Assert.ThrowsAsync<PlanningModelException>(() =>
            model.ReplyAsync(new PlanningModelRequest("Hello", null), CancellationToken.None));

        Assert.Equal(1, handler.Calls);
        var logged = string.Join(" ", logger.Messages);
        Assert.Contains("redacted/redacted", logged, StringComparison.Ordinal);
        Assert.DoesNotContain("99999", logged, StringComparison.Ordinal);
        Assert.DoesNotContain("dp:v1:", logged, StringComparison.Ordinal);
        Assert.DoesNotContain(ApiKey, logged, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReplyAsync_PropagatesCallerCancellation()
    {
        var handler = new RecordingHandler();
        var model = CreateModel(handler, ConfiguredOptions());
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            model.ReplyAsync(new PlanningModelRequest("Hello", null), cancellation.Token));
        Assert.True(handler.Calls <= 1);
    }

    [Fact]
    public async Task ReplyAsync_RefusesABankTokenBeforeCallingOpenAi()
    {
        var handler = new RecordingHandler();
        var logger = new ListLogger();
        var model = CreateModel(handler, ConfiguredOptions(), logger);
        const string token = "access-sandbox-do-not-send";

        var input = await Assert.ThrowsAsync<BadRequestException>(() =>
            model.ReplyAsync(new PlanningModelRequest("See " + token, "Stay brief."), CancellationToken.None));
        var instructions = await Assert.ThrowsAsync<BadRequestException>(() =>
            model.ReplyAsync(
                new PlanningModelRequest("What is next?", "dp:v1:protected-token"),
                CancellationToken.None));

        Assert.Equal(PlanningShareRules.TokenRefusal, input.Message);
        Assert.Equal(PlanningShareRules.TokenRefusal, instructions.Message);
        Assert.Null(handler.Request);
        Assert.DoesNotContain(logger.Messages, message => message.Contains(token, StringComparison.Ordinal));
        Assert.DoesNotContain(logger.Messages, message => message.Contains("dp:v1:", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ReplyAsync_SendsOrdinaryAccessWording()
    {
        var handler = new RecordingHandler();
        var model = CreateModel(handler, ConfiguredOptions());

        var reply = await model.ReplyAsync(
            new PlanningModelRequest("Please access the savings account.", "Stay brief."),
            CancellationToken.None);

        Assert.Equal("Keep the minimum.", reply.Text);
        Assert.NotNull(handler.Request);
        Assert.Contains("Please access the savings account.", handler.Body, StringComparison.Ordinal);
    }

    #region Private Methods

    /// <summary>
    /// Builds the adapter around a handler so the test never reaches OpenAI.
    /// </summary>
    private static OpenAiPlanningModel CreateModel(
        RecordingHandler handler,
        PlanningModelOptions options,
        ILogger<OpenAiPlanningModel>? logger = null)
    {
        var client = new HttpClient(handler)
        {
            BaseAddress = PlanningModelConfiguration.Endpoint
        };
        return new OpenAiPlanningModel(
            client,
            Microsoft.Extensions.Options.Options.Create(options),
            logger ?? new ListLogger());
    }

    /// <summary>
    /// A complete configuration that does not name a priced model.
    /// </summary>
    private static PlanningModelOptions ConfiguredOptions()
    {
        return new PlanningModelOptions
        {
            ApiKey = ApiKey,
            Model = " configured-model "
        };
    }

    #endregion

    /// <summary>
    /// Captures one request and returns a canned response.
    /// </summary>
    private sealed class RecordingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        public int Calls { get; private set; }

        public List<string> Bodies { get; } = [];

        public string Body => Bodies.LastOrDefault() ?? "";

        public List<ScriptedCall> Script { get; } = [];

        public HttpStatusCode StatusCode { get; init; } = HttpStatusCode.OK;

        public string ResponseBody { get; init; } = """{"output_text":"Keep the minimum."}""";

        /// <summary>
        /// Records the outgoing request, including its body, and returns the canned response.
        /// A scripted call is used when one was queued for this attempt.
        /// </summary>
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Calls++;
            Request = request;
            Bodies.Add(request.Content is null
                ? ""
                : await request.Content.ReadAsStringAsync(cancellationToken));
            if (Script.Count > 0)
            {
                var scripted = Script[Math.Min(Calls - 1, Script.Count - 1)];
                if (scripted.Error is not null)
                {
                    throw scripted.Error;
                }

                return new HttpResponseMessage(scripted.StatusCode)
                {
                    Content = new StringContent(scripted.Body, Encoding.UTF8, "application/json")
                };
            }

            return new HttpResponseMessage(StatusCode)
            {
                Content = new StringContent(ResponseBody, Encoding.UTF8, "application/json")
            };
        }
    }

    /// <summary>
    /// One canned provider result. Error is thrown instead of returning Body.
    /// </summary>
    private sealed record ScriptedCall(
        HttpStatusCode StatusCode,
        string Body,
        Exception? Error = null);

    /// <summary>
    /// Keeps formatted log lines so a test can check that a secret was not written.
    /// </summary>
    private sealed class ListLogger : ILogger<OpenAiPlanningModel>
    {
        public List<string> Messages { get; } = [];

        /// <inheritdoc />
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }

        /// <inheritdoc />
        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        /// <inheritdoc />
        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
            if (exception is not null)
            {
                Messages.Add(exception.ToString());
            }
        }
    }
}
