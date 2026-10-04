using Cardui.Api.Dtos.Transaction;

namespace Cardui.Api.Dtos.Dashboard;

public class DashboardSummaryDto
{
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public decimal CashBalance { get; set; }
    public decimal CreditCardBalance { get; set; }
    public decimal NetWorth { get; set; }
    public decimal MonthlyIncome { get; set; }
    public decimal MonthlySpending { get; set; }
    public string PlanningCurrency { get; set; } = "USD";
    public int ExcludedAccountCount { get; set; }
    public int ExcludedTransactionCount { get; set; }
    public IReadOnlyList<string> ExcludedCurrencies { get; set; } = [];
    public IReadOnlyList<TransactionDto> RecentTransactions { get; set; } = [];
    public IReadOnlyList<SpendingByCategoryDto> SpendingByCategory { get; set; } = [];
}