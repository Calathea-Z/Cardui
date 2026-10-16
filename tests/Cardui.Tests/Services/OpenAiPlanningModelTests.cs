using System.Net;
using System.Text;
using System.Text.Json;
using Cardui.Api.Configuration;
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

        public string Body { get; private set; } = "";

        public HttpStatusCode StatusCode { get; init; } = HttpStatusCode.OK;

        public string ResponseBody { get; init; } = """{"output_text":"Keep the minimum."}""";

        /// <summary>
        /// Records the outgoing request, including its body, and returns the canned response.
        /// </summary>
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Request = request;
            Body = request.Content is null
                ? ""
                : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(StatusCode)
            {
                Content = new StringContent(ResponseBody, Encoding.UTF8, "application/json")
            };
        }
    }

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
