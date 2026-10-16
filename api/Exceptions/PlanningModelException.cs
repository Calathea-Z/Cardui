namespace Cardui.Api.Exceptions;

using Microsoft.AspNetCore.Http;

public sealed class PlanningModelException : AppException
{
    public PlanningModelException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }

    /// <inheritdoc />
    public override int StatusCode => StatusCodes.Status502BadGateway;

    /// <inheritdoc />
    public override string Title => "Planning Model Error";
}
