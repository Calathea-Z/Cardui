namespace Cardui.Api.Dtos.Account;

public class AccountBalanceHistoryPointDto
{
    public DateOnly Date { get; set; }
    public decimal NetWorth { get; set; }
    public decimal Cash { get; set; }
    public decimal Investments { get; set; }
    public decimal CreditCards { get; set; }
    public decimal Loans { get; set; }
}