namespace Cardui.Api.Exceptions;

public sealed class PlaidWebhookRejectedException : AppException
{
    public PlaidWebhookRejectedException()
        : base("The Plaid webhook could not be verified.")
    {
    }

    /// <inheritdoc />
    public override int StatusCode => StatusCodes.Status401Unauthorized;

    /// <inheritdoc />
    public override string Title => "Unauthorized";
}
