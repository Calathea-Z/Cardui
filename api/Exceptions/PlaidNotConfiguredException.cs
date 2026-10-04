namespace Cardui.Api.Exceptions;

using Microsoft.AspNetCore.Http;

public sealed class PlaidNotConfiguredException : AppException
{
    public PlaidNotConfiguredException()
        : base("Bank linking is not configured.")
    {
    }

    /// <inheritdoc />
    public override int StatusCode => StatusCodes.Status503ServiceUnavailable;

    /// <inheritdoc />
    public override string Title => "Plaid Not Configured";
}
