using Cardui.Api.Data;
using Cardui.Api.Domain;
using Cardui.Api.Dtos.Dashboard;
using Cardui.Api.Dtos.Transaction;
using Cardui.Api.Mapping;
using Cardui.Api.Models;
using Cardui.Api.Security;
using Cardui.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cardui.Api.Services.Implementations;

public class DashboardService : IDashboardService
{
    private const int RecentTransactionCount = 8;

    private readonly CarduiDBContext _dbContext;
    private readonly TimeProvider _timeProvider;
    private readonly HouseholdScope _householdScope;

    public DashboardService(
        CarduiDBContext dbContext,
        TimeProvider timeProvider,
        HouseholdScope householdScope)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
        _householdScope = householdScope;
    }

    /// <inheritdoc />
    public async Task<DashboardSummaryDto> GetSummaryAsync(
        CancellationToken cancellationToken = default)
    {
        var (monthStart, monthEnd) = GetCurrentMonthRange();

        var accountTotals = await GetActiveAccountTotalsAsync(cancellationToken);
        var monthlyActivity = await GetMonthlyActivityAsync(
            monthStart,
            monthEnd,
            cancellationToken);
        var recentTransactions = await GetRecentTransactionsAsync(cancellationToken);

        return new DashboardSummaryDto
        {
            PeriodStart = monthStart,
            PeriodEnd = monthEnd,
            CashBalance = accountTotals.Cash,
            CreditCardBalance = accountTotals.CreditCards,
            NetWorth = accountTotals.NetWorth,
            MonthlyIncome = monthlyActivity.Income,
            MonthlySpending = monthlyActivity.Spending,
            RecentTransactions = recentTransactions,
            SpendingByCategory = monthlyActivity.SpendingByCategory
                .Select(x => new SpendingByCategoryDto
                {
                    CategoryId = x.CategoryId,
                    CategoryName = x.CategoryName,
                    Color = x.CategoryColor,
                    Amount = x.Amount
                })
                .ToList()
        };
    }

    #region Private Methods

    /// <summary>
    /// Returns the first day of the local month through today.
    /// </summary>
    private (DateOnly Start, DateOnly End) GetCurrentMonthRange()
    {
        var today = FinancialDate.Today(_timeProvider);
        var monthStart = new DateOnly(today.Year, today.Month, 1);

        return (monthStart, today);
    }

    /// <summary>
    /// Sums cash, investments, credit cards, loans, and net worth for
    /// active accounts that are not archived.
    /// </summary>
    private async Task<AccountTotals> GetActiveAccountTotalsAsync(
        CancellationToken cancellationToken)
    {
        var balances = await _dbContext.Accounts
            .AsNoTracking()
            .InHousehold(_householdScope)
            .Where(x => x.IsActive && x.ArchivedAt == null)
            .Select(x => new AccountBalanceValue(x.Type, x.CurrentBalance))
            .ToListAsync(cancellationToken);

        return AccountTotalsCalculator.Calculate(balances);
    }

    /// <summary>
    /// Totals income and spending for posted, non-archived transactions
    /// in the date range. Transfers and reconciliations are excluded by the calculator.
    /// </summary>
    private async Task<TransactionActivityTotals> GetMonthlyActivityAsync(
        DateOnly monthStart,
        DateOnly monthEnd,
        CancellationToken cancellationToken)
    {
        var transactions = await TransactionsInDateRange(monthStart, monthEnd)
            .AsNoTracking()
            .Where(x => x.ArchivedAt == null)
            .Select(x => new TransactionActivityValue(
                x.Amount,
                x.Pending,
                x.CategoryId,
                x.Category == null
                    ? SystemCategoryNames.Uncategorized
                    : x.Category.Name,
                x.Category == null ? null : x.Category.Color,
                x.Category == null ? null : x.Category.Key,
                x.Category == null ? null : x.Category.SubGroup.Group.Key,
                x.Provenance))
            .ToListAsync(cancellationToken);

        return TransactionActivityCalculator.Calculate(transactions);
    }

    /// <summary>
    /// Returns the eight most recent non-archived transactions.
    /// Transactions on the same date are ordered by when they were created.
    /// </summary>
    private async Task<IReadOnlyList<TransactionDto>> GetRecentTransactionsAsync(
        CancellationToken cancellationToken)
    {
        return await _dbContext.Transactions
            .AsNoTracking()
            .InHousehold(_dbContext, _householdScope)
            .Where(x => x.ArchivedAt == null)
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.CreatedAt)
            .Take(RecentTransactionCount)
            .Select(TransactionDtoMapper.Projection)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Household transactions whose date falls inside the inclusive range.
    /// </summary>
    private IQueryable<Transaction> TransactionsInDateRange(DateOnly start, DateOnly end)
    {
        return _dbContext.Transactions
            .InHousehold(_dbContext, _householdScope)
            .Where(x => x.Date >= start && x.Date <= end);
    }

    #endregion
}
