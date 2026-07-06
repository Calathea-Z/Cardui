namespace Cardui.Api.Dtos.Account;

public class AccountGroupDto
{
    public required string Key { get; set; }
    public required string Name { get; set; }
    public decimal Total { get; set; }
    public IReadOnlyList<AccountDto> Accounts { get; set; } = [];
}