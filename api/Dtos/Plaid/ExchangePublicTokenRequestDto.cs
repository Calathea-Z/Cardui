namespace Cardui.Api.Dtos.Plaid;

public class ExchangePublicTokenRequestDto
{
    public required string PublicToken { get; set; }
    public string? InstitutionId { get; set; }
    public string? InstitutionName { get; set; }
}