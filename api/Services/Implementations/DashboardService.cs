using Cardui.Api.Data;
using Cardui.Api.Domain;
using Cardui.Api.Dtos.Dashboard;
using Cardui.Api.Dtos.Transaction;
using Cardui.Api.Mapping;
using Cardui.Api.Models;
using Cardui.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cardui.Api.Services.Implementations;

public class DashboardService : IDashboardService
{
    private const int RecentTransactionCount = 8;

    private readonly CarduiDBContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public DashboardService(CarduiDBContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(
        CancellationToken cancellationToken = default)
    {
        var (monthStart, monthEnd) = GetCurrentMonthRange();

        var cashBalance = await GetActiveAccountBalanceAsync(
            AccountTypes.Depository,
            cancellationToken);
        var creditCardBalance = await GetActiveAccountBalanceAsync(
            AccountTypes.Credit,
            cancellationToken);
        var (monthlyIncome, monthlySpending) = await GetMonthlyActivityAsync(
            monthStart,
            monthEnd,
            cancellationToken);
        var recentTransactions = await GetRecentTransactionsAsync(cancellationToken);
        var spendingByCategory = await GetSpendingByCategoryAsync(
            monthStart,
            monthEnd,
            cancellationToken);

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

    private (DateOnly Start, DateOnly End) GetCurrentMonthRange()
    {
        var today = DateOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime);
        var monthStart = new DateOnly(today.Year, today.Month, 1);

        return (monthStart, today);
    }

    private async Task<decimal> GetActiveAccountBalanceAsync(
        string accountType,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Accounts
            .AsNoTracking()
            .Where(x => x.IsActive && x.Type == accountType)
            .SumAsync(x => x.CurrentBalance, cancellationToken);
    }

    private async Task<(decimal Income, decimal Spending)> GetMonthlyActivityAsync(
        DateOnly monthStart,
        DateOnly monthEnd,
        CancellationToken cancellationToken)
    {
        var totals = await TransactionsInDateRange(monthStart, monthEnd)
            .AsNoTracking()
            .Where(t => t.Category == null || t.Category.Key != SystemCategoryKeys.Transfers)
            .GroupBy(_ => 1)
            .Select(x => new
            {
                Income = x.Where(t => t.Amount < 0).Sum(t => -t.Amount),
                Spending = x.Where(t => t.Amount > 0).Sum(t => t.Amount)
            })
            .FirstOrDefaultAsync(cancellationToken);

        return (totals?.Income ?? 0, totals?.Spending ?? 0);
    }

    private async Task<IReadOnlyList<TransactionDto>> GetRecentTransactionsAsync(
        CancellationToken cancellationToken)
    {
        return await _dbContext.Transactions
            .AsNoTracking()
            .OrderByDescending(x => x.Date)
            .Take(RecentTransactionCount)
            .Select(TransactionDtoMapper.Projection)
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<SpendingByCategoryDto>> GetSpendingByCategoryAsync(
        DateOnly monthStart,
        DateOnly monthEnd,
        CancellationToken cancellationToken)
    {
        return await TransactionsInDateRange(monthStart, monthEnd)
            .AsNoTracking()
            .Where(x => x.Amount > 0)
            .Where(x => x.Category == null || x.Category.Key != SystemCategoryKeys.Transfers)
            .GroupBy(x => new
            {
                x.CategoryId,
                CategoryName = x.Category == null ? SystemCategoryNames.Uncategorized : x.Category.Name,
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
            .ToListAsync(cancellationToken);
    }

    private IQueryable<Transaction> TransactionsInDateRange(DateOnly start, DateOnly end)
    {
        return _dbContext.Transactions
            .Where(x => x.Date >= start && x.Date <= end);
    }
}
