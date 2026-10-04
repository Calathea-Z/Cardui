namespace Cardui.Api.Exceptions;

using Microsoft.AspNetCore.Http;

public abstract class AppException : Exception
{
    protected AppException(string message) : base(message)
    {
    }

    protected AppException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// HTTP status returned for this error.
    /// </summary>
    public abstract int StatusCode { get; }

    /// <summary>
    /// Short problem-details title for the status code.
    /// </summary>
    public virtual string Title => StatusCode switch
    {
        StatusCodes.Status400BadRequest => "Bad Request",
        StatusCodes.Status404NotFound => "Not Found",
        _ => "Error"
    };
}

public sealed class NotFoundException : AppException
{
    public NotFoundException(string message) : base(message)
    {
    }

    /// <inheritdoc />
    public override int StatusCode => StatusCodes.Status404NotFound;
}

public sealed class BadRequestException : AppException
{
    public BadRequestException(string message) : base(message)
    {
    }

    /// <inheritdoc />
    public override int StatusCode => StatusCodes.Status400BadRequest;
}

public sealed class PlaidSyncException : AppException
{
    public PlaidSyncException(
        string message,
        string? plaidErrorCode = null,
        string? plaidErrorType = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        PlaidErrorCode = plaidErrorCode;
        PlaidErrorType = plaidErrorType;
    }

    public string? PlaidErrorCode { get; }

    public string? PlaidErrorType { get; }

    /// <summary>
    /// Maps a Plaid error to 409, 429, 400, or 502.
    /// </summary>
    public override int StatusCode => PlaidErrorCode switch
    {
        "ITEM_LOGIN_REQUIRED" => StatusCodes.Status409Conflict,
        "RATE_LIMIT_EXCEEDED" => StatusCodes.Status429TooManyRequests,
        _ when string.Equals(PlaidErrorType, "INVALID_INPUT", StringComparison.OrdinalIgnoreCase)
            => StatusCodes.Status400BadRequest,
        _ => StatusCodes.Status502BadGateway
    };

    /// <summary>
    /// Problem-details title for the Plaid status chosen above.
    /// </summary>
    public override string Title => StatusCode switch
    {
        StatusCodes.Status409Conflict => "Plaid Item Requires Reauthentication",
        StatusCodes.Status429TooManyRequests => "Plaid Rate Limit Exceeded",
        StatusCodes.Status400BadRequest => "Plaid Request Invalid",
        StatusCodes.Status502BadGateway => "Plaid Service Error",
        _ => base.Title
    };
}
