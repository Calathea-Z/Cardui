using Cardui.Api.Data;
using Cardui.Api.Domain;
using Cardui.Api.Domain.Accounts;
using Cardui.Api.Domain.Categories;
using Cardui.Api.Domain.Transactions;
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
            CashBalance = accountTotals.Totals.Cash,
            CreditCardBalance = accountTotals.Totals.CreditCards,
            NetWorth = accountTotals.Totals.NetWorth,
            MonthlyIncome = monthlyActivity.Totals.Income,
            MonthlySpending = monthlyActivity.Totals.Spending,
            PlanningCurrency = _householdScope.PlanningCurrency,
            ExcludedAccountCount = accountTotals.ExcludedAccountCount,
            ExcludedTransactionCount = monthlyActivity.ExcludedTransactionCount,
            ExcludedCurrencies = accountTotals.ExcludedCurrencies
                .Concat(monthlyActivity.ExcludedCurrencies)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(code => code, StringComparer.Ordinal)
                .ToList(),
            RecentTransactions = recentTransactions,
            SpendingByCategory = monthlyActivity.Totals.SpendingByCategory
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
        var today = FinancialDate.Today(_timeProvider, _householdScope.TimeZoneId);
        var monthStart = new DateOnly(today.Year, today.Month, 1);

        return (monthStart, today);
    }

    /// <summary>
    /// Sums cash, investments, credit cards, loans, and net worth for
    /// active accounts that are not archived and use the planning currency.
    /// </summary>
    private async Task<AccountTotalResult> GetActiveAccountTotalsAsync(
        CancellationToken cancellationToken)
    {
        var planningCurrency = _householdScope.PlanningCurrency;
        var balances = await _dbContext.Accounts
            .AsNoTracking()
            .InHousehold(_householdScope)
            .Where(x => x.IsActive && x.ArchivedAt == null)
            .Select(x => new AccountCurrencyBalance(
                x.Type,
                x.CurrentBalance,
                x.IsoCurrencyCode))
            .ToListAsync(cancellationToken);

        var included = balances
            .Where(x => PlanningCurrencyRules.IsIncluded(x.CurrencyCode, planningCurrency))
            .Select(x => new AccountBalanceValue(x.Type, x.CurrentBalance));
        var excluded = balances
            .Where(x => !PlanningCurrencyRules.IsIncluded(x.CurrencyCode, planningCurrency))
            .ToList();

        return new AccountTotalResult(
            AccountTotalsCalculator.Calculate(included),
            excluded.Count,
            PlanningCurrencyRules.ExcludedCodes(
                excluded.Select(x => x.CurrencyCode),
                planningCurrency));
    }

    /// <summary>
    /// Totals income and spending for posted, non-archived transactions
    /// in the date range that use the planning currency.
    /// Transfers and reconciliations are excluded by the calculator.
    /// </summary>
    private async Task<MonthlyActivityResult> GetMonthlyActivityAsync(
        DateOnly monthStart,
        DateOnly monthEnd,
        CancellationToken cancellationToken)
    {
        var planningCurrency = _householdScope.PlanningCurrency;
        var rows = await TransactionsInDateRange(monthStart, monthEnd)
            .AsNoTracking()
            .Where(x => x.ArchivedAt == null)
            .Select(x => new ActivityRow(
                x.Amount,
                x.Pending,
                x.CategoryId,
                x.Category == null
                    ? SystemCategoryNames.Uncategorized
                    : x.Category.Name,
                x.Category == null ? null : x.Category.Color,
                x.Category == null ? null : x.Category.Key,
                x.Category == null ? null : x.Category.SubGroup.Group.Key,
                x.Provenance,
                x.IsoCurrencyCode))
            .ToListAsync(cancellationToken);

        var included = new List<TransactionActivityValue>();
        var excludedCurrencies = new List<string?>();
        var excludedCount = 0;

        foreach (var row in rows)
        {
            var value = row.ToValue();
            if (PlanningCurrencyRules.IsIncluded(row.CurrencyCode, planningCurrency))
            {
                included.Add(value);
                continue;
            }

            if (TransactionActivityCalculator.AffectsIncomeOrSpending(value))
            {
                excludedCount++;
                excludedCurrencies.Add(row.CurrencyCode);
            }
        }

        return new MonthlyActivityResult(
            TransactionActivityCalculator.Calculate(included),
            excludedCount,
            PlanningCurrencyRules.ExcludedCodes(excludedCurrencies, planningCurrency));
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
