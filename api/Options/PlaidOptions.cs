namespace Cardui.Api.Options;

public class PlaidOptions
{
    public string ClientId { get; set; } = "";
    public string Secret { get; set; } = "";
    public string Environment { get; set; } = "";
    public string ClientName { get; set; } = "Cardui";
    public string DefaultClientUserId { get; set; } = "dev-user";

    public string? WebhookUrl { get; set; }
}