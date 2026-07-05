namespace Cardui.Api.Exceptions;

using Microsoft.AspNetCore.Http;

public abstract class AppException : Exception
{
    protected AppException(string message) : base(message)
    {
    }

    public abstract int StatusCode { get; }

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

    public override int StatusCode => StatusCodes.Status404NotFound;
}

public sealed class BadRequestException : AppException
{
    public BadRequestException(string message) : base(message)
    {
    }

    public override int StatusCode => StatusCodes.Status400BadRequest;
}
