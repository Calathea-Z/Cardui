using System.Globalization;

namespace Cardui.Api.Domain.Planning;

/// <summary>
/// Bounds one planning-model call.
/// A transient failure can be tried again. The input and the output are capped.
/// No dollar price is chosen. A log line keeps the attempt, the status, and token counts.
/// </summary>
public static class PlanningCallRules
{
    /// <summary>
    /// How many times one call may be posted. The first try counts. Two retries remain after it.
    /// </summary>
    public const int MaxAttempts = 3;

    /// <summary>
    /// The most output tokens one request may ask for.
    /// This caps the reply length. It is not a price.
    /// </summary>
    public const int MaxOutputTokens = 1024;

    /// <summary>
    /// The longest message or developer text that may be posted, in characters.
    /// Spaces at the ends do not count. A longer text is not sent.
    /// </summary>
    public const int MaxInputCharacters = 24000;

    /// <summary>
    /// The longest provider error code that may be written to a log.
    /// A sentence, a bank token, or a longer value is left out.
    /// </summary>
    public const int LoggedCodeMaxLength = 80;

    /// <summary>
    /// Pause before a retry. The caller cancelling during the pause stops the call.
    /// </summary>
    public static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Refusal used when the message or the developer text is past the character cap.
    /// </summary>
    public const string InputTooLong = "This request is too long to send.";

    /// <summary>
    /// Notice used when the text was past the cap. The draft is left unchanged.
    /// </summary>
    public const string LimitNotice =
        "This request is too long to send. Your draft is unchanged. You can continue on Plan.";

    /// <summary>
    /// Notice used when the provider did not answer. The draft is left unchanged.
    /// </summary>
    public const string UnavailableNotice =
        "The planning model did not answer. Your draft is unchanged. You can continue on Plan.";

    /// <summary>
    /// Notice used when guided conversation is off. The draft is left unchanged.
    /// </summary>
    public const string NotConfiguredNotice =
        "Guided conversation is not available. Your draft is unchanged. You can continue on Plan.";

    /// <summary>
    /// True when this HTTP status is worth one more try.
    /// A completed response, including an empty one, is not transient.
    /// </summary>
    public static bool IsTransient(int statusCode)
    {
        return statusCode is 408 or 429 or 500 or 502 or 503 or 504;
    }

    /// <summary>
    /// True when another attempt is still inside the limit.
    /// The attempt number starts at 1.
    /// </summary>
    public static bool CanRetry(int attempt)
    {
        return attempt < MaxAttempts;
    }

    /// <summary>
    /// Refuses a bank access token in the message or the developer text.
    /// Ordinary words such as "access the account" are not a token.
    /// </summary>
    public static string? RejectToken(string? input, string? instructions)
    {
        if (PlanningShareRules.ContainsToken(input)
            || PlanningShareRules.ContainsToken(instructions))
        {
            return PlanningShareRules.TokenRefusal;
        }

        return null;
    }

    /// <summary>
    /// Refuses text past the character cap. A blank value is not a length error.
    /// Spaces at the ends do not count.
    /// </summary>
    public static string? RejectLength(string? input, string? instructions)
    {
        if (PastCap(input) || PastCap(instructions))
        {
            return InputTooLong;
        }

        return null;
    }

    /// <summary>
    /// Keeps a short provider code. A sentence, a bank token, or a longer value becomes "redacted".
    /// A blank value becomes "none".
    /// </summary>
    public static string LoggedCode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "none";
        }

        var trimmed = value.Trim();
        if (trimmed.Length > LoggedCodeMaxLength
            || trimmed.Any(char.IsWhiteSpace)
            || PlanningShareRules.ContainsToken(trimmed))
        {
            return "redacted";
        }

        return trimmed;
    }

    /// <summary>
    /// Describes one attempt that will be tried again.
    /// The line has the attempt, the status, and the safe codes. It has no message text.
    /// </summary>
    public static string DescribeRetry(
        int attempt,
        int? statusCode,
        string? errorType,
        string? errorCode,
        string? exceptionType)
    {
        return
            $"Planning model will retry after attempt {attempt}. {DescribeStatus(statusCode, errorType, errorCode, exceptionType)}.";
    }

    /// <summary>
    /// Describes one attempt that will not be tried again.
    /// The line has the attempt, the status, and the safe codes. It has no message text.
    /// </summary>
    public static string DescribeFailure(
        int attempt,
        int? statusCode,
        string? errorType,
        string? errorCode,
        string? exceptionType)
    {
        return
            $"Planning model request failed on attempt {attempt}. {DescribeStatus(statusCode, errorType, errorCode, exceptionType)}.";
    }

    /// <summary>
    /// Describes a reply that was read.
    /// Token counts are numbers. A missing count says usage was not reported. The reply text is left out.
    /// </summary>
    public static string DescribeAnswer(int attempt, int? inputTokens, int? outputTokens)
    {
        if (inputTokens is null && outputTokens is null)
        {
            return $"Planning model answered on attempt {attempt}. Usage was not reported.";
        }

        return
            $"Planning model answered on attempt {attempt}. Input tokens {Token(inputTokens)}. Output tokens {Token(outputTokens)}.";
    }

    /// <summary>
    /// Describes a call that left the draft unchanged.
    /// The reason is one of the fallback names. The person's text is left out.
    /// </summary>
    public static string DescribeFallback(PlanningCallFallback reason)
    {
        return $"Planning model call fell back to manual planning. Reason {reason}.";
    }

    #region Private Methods

    /// <summary>
    /// True when the trimmed text is past the character cap.
    /// </summary>
    private static bool PastCap(string? text)
    {
        return !string.IsNullOrWhiteSpace(text)
            && text.Trim().Length > MaxInputCharacters;
    }

    /// <summary>
    /// Joins the status and the safe codes for one attempt.
    /// </summary>
    private static string DescribeStatus(
        int? statusCode,
        string? errorType,
        string? errorCode,
        string? exceptionType)
    {
        var status = statusCode?.ToString(CultureInfo.InvariantCulture) ?? "none";
        return
            $"Status {status}. {LoggedCode(errorType)}/{LoggedCode(errorCode)}. {LoggedCode(exceptionType)}";
    }

    /// <summary>
    /// Prints a token count. A missing count stays unknown.
    /// </summary>
    private static string Token(int? value)
    {
        return value?.ToString(CultureInfo.InvariantCulture) ?? "none";
    }

    #endregion
}
