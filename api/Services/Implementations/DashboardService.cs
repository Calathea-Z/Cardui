using Cardui.Api.Data;
using Cardui.Api.Dtos.Dashboard;
using Cardui.Api.Dtos.Transaction;
using Cardui.Api.Mapping;
using Cardui.Api.Models;
using Cardui.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cardui.Api.Services.Implementations;

public class DashboardService : IDashboardService
{
    private const string DepositoryAccountType = "depository";
    private const string CreditAccountType = "credit";
    private const string UncategorizedCategoryName = "Uncategorized";
    private const string TransfersCategoryKey = "transfers";
    private const int RecentTransactionCount = 8;

    private readonly CarduiDBContext _dbContext;

    public DashboardService(CarduiDBContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync()
    {
        var (monthStart, monthEnd) = GetCurrentMonthRange();

        var cashBalance = await GetActiveAccountBalanceAsync(DepositoryAccountType);
        var creditCardBalance = await GetActiveAccountBalanceAsync(CreditAccountType);
        var (monthlyIncome, monthlySpending) = await GetMonthlyActivityAsync(monthStart, monthEnd);
        var recentTransactions = await GetRecentTransactionsAsync();
        var spendingByCategory = await GetSpendingByCategoryAsync(monthStart, monthEnd);

        return new DashboardSummaryDto
        {
            CashBalance = cashBalance,
            CreditCardBalance = creditCardBalance,
            NetWorth = cashBalance - creditCardBalance,
            MonthlyIncome = monthlyIncome,
            MonthlySpending = monthlySpending,
            RecentTransactions = recentTransactions,
            SpendingByCategory = spendingByCategory
        };
    }

    #region Private Methods

    private static (DateOnly Start, DateOnly End) GetCurrentMonthRange()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var monthStart = new DateOnly(today.Year, today.Month, 1);

        return (monthStart, today);
    }

    private async Task<decimal> GetActiveAccountBalanceAsync(string accountType)
    {
        return await _dbContext.Accounts
            .AsNoTracking()
            .Where(x => x.IsActive && x.Type == accountType)
            .SumAsync(x => x.CurrentBalance);
    }

    private async Task<(decimal Income, decimal Spending)> GetMonthlyActivityAsync(
        DateOnly monthStart,
        DateOnly monthEnd)
    {
        var totals = await TransactionsInDateRange(monthStart, monthEnd)
            .AsNoTracking()
            .Where(t => t.Category == null || t.Category.Key != TransfersCategoryKey)
            .GroupBy(_ => 1)
            .Select(x => new
            {
                Income = x.Where(t => t.Amount < 0).Sum(t => -t.Amount),
                Spending = x.Where(t => t.Amount > 0).Sum(t => t.Amount)
            })
            .FirstOrDefaultAsync();

        return (totals?.Income ?? 0, totals?.Spending ?? 0);
    }

    private async Task<IReadOnlyList<TransactionDto>> GetRecentTransactionsAsync()
    {
        return await _dbContext.Transactions
            .AsNoTracking()
            .OrderByDescending(x => x.Date)
            .Take(RecentTransactionCount)
            .Select(TransactionDtoMapper.Projection)
            .ToListAsync();
    }

    private async Task<IReadOnlyList<SpendingByCategoryDto>> GetSpendingByCategoryAsync(
        DateOnly monthStart,
        DateOnly monthEnd)
    {
        return await TransactionsInDateRange(monthStart, monthEnd)
            .AsNoTracking()
            .Where(x => x.Amount > 0)
            .Where(x => x.Category == null || x.Category.Key != TransfersCategoryKey)
            .GroupBy(x => new
            {
                x.CategoryId,
                CategoryName = x.Category == null ? UncategorizedCategoryName : x.Category.Name,
                Color = x.Category == null ? null : x.Category.Color
            })
            .Select(x => new SpendingByCategoryDto
            {
                CategoryId = x.Key.CategoryId,
                CategoryName = x.Key.CategoryName,
                Color = x.Key.Color,
                Amount = x.Sum(t => t.Amount)
            })
            .OrderByDescending(x => x.Amount)
            .ToListAsync();
    }

    private IQueryable<Transaction> TransactionsInDateRange(DateOnly start, DateOnly end)
    {
        return _dbContext.Transactions
            .Where(x => x.Date >= start && x.Date <= end);
    }

    #endregion
}
