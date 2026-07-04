using Cardui.Api.Dtos.Transaction;

namespace Cardui.Api.Dtos.Dashboard;

public class DashboardSummaryDto
{
    public decimal CashBalance { get; set; }
    public decimal CreditCardBalance { get; set; }
    public decimal NetWorth { get; set; }
    public decimal MonthlyIncome { get; set; }
    public decimal MonthlySpending { get; set; }
    public IReadOnlyList<TransactionDto> RecentTransactions { get; set; } = [];
    public IReadOnlyList<SpendingByCategoryDto> SpendingByCategory { get; set; } = [];
}