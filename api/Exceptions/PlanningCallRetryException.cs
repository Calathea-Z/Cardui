namespace Cardui.Api.Exceptions;

/// <summary>
/// One planning-model attempt failed in a way that can be tried again.
/// The message is fixed. The status and the provider codes are the only details, and they are not the response body.
/// </summary>
internal sealed class PlanningCallRetryException : Exception
{
    public PlanningCallRetryException(
        int? statusCode,
        string? errorType,
        string? errorCode,
        string? exceptionType)
        : base("The planning model attempt can be retried.")
    {
        StatusCode = statusCode;
        ErrorType = errorType;
        ErrorCode = errorCode;
        ExceptionType = exceptionType;
    }

    public int? StatusCode { get; }

    public string? ErrorType { get; }

    public string? ErrorCode { get; }

    public string? ExceptionType { get; }
}
