using Microsoft.AspNetCore.Http;

namespace Cardui.Api.Exceptions;

/// <summary>
/// The request conflicts with the saved row it is trying to change.
/// A repeated accept of the accepted plan is not a conflict.
/// </summary>
public sealed class ConflictException : AppException
{
    public ConflictException(string message) : base(message)
    {
    }

    /// <inheritdoc />
    public override int StatusCode => StatusCodes.Status409Conflict;

    /// <inheritdoc />
    public override string Title => "Conflict";
}
