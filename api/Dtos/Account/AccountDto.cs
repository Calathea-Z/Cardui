namespace Cardui.Api.Dtos.Account;

public class AccountDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? OfficialName { get; set; }
    public required string Type { get; set; }
    public string? Subtype { get; set; }
    public string? Mask { get; set; }
    public decimal CurrentBalance { get; set; }
    public decimal? AvailableBalance { get; set; }
    public string? IsoCurrencyCode { get; set; }
    public bool IsActive { get; set; }
}