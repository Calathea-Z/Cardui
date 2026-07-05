namespace Cardui.Api.Options;

public class PlaidOptions
{
    public required string ClientId { get; set; }
    public required string Secret { get; set; }
    public required string Environment { get; set; }
    public string ClientName { get; set; } = "Cardui";
}