namespace Cardui.Api.Dtos.Account;

public class AccountSummaryDto
{
    public decimal NetWorth { get; set; }
    public string PlanningCurrency { get; set; } = "USD";
    public int ExcludedAccountCount { get; set; }
    public IReadOnlyList<string> ExcludedCurrencies { get; set; } = [];
    public IReadOnlyList<AccountGroupDto> Groups { get; set; } = [];
    public IReadOnlyList<AccountBalanceHistoryPointDto> History { get; set; } = [];
    public IReadOnlyList<AccountDto> ArchivedAccounts { get; set; } = [];
}