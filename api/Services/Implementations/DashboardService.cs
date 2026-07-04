using Cardui.Api.Data;
using Cardui.Api.Dtos.Dashboard;
using Cardui.Api.Dtos.Transaction;
using Cardui.Api.Models;
using Cardui.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Cardui.Api.Services.Implementations;

public class DashboardService : IDashboardService
{
    private const string DepositoryAccountType = "depository";
    private const string CreditAccountType = "credit";
    private const string UncategorizedCategoryName = "Uncategorized";
    private const int RecentTransactionCount = 8;

    private readonly CarduiDBContext _dbContext;

    public DashboardService(CarduiDBContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync()
    {
        var currentMonth = DateRange.CurrentMonth();

        var cashBalance = await GetActiveAccountBalanceAsync(DepositoryAccountType);
        var creditCardBalance = await GetActiveAccountBalanceAsync(CreditAccountType);
        var monthlyActivity = await GetMonthlyActivityAsync(currentMonth);
        var recentTransactions = await GetRecentTransactionsAsync();
        var spendingByCategory = await GetSpendingByCategoryAsync(currentMonth);

        return new DashboardSummaryDto
        {
            CashBalance = cashBalance,
            CreditCardBalance = creditCardBalance,
            NetWorth = cashBalance - creditCardBalance,
            MonthlyIncome = monthlyActivity.Income,
            MonthlySpending = monthlyActivity.Spending,
            RecentTransactions = recentTransactions,
            SpendingByCategory = spendingByCategory
        };
    }

    private async Task<decimal> GetActiveAccountBalanceAsync(string accountType)
    {
        return await _dbContext.Accounts
            .AsNoTracking()
            .Where(x => x.IsActive && x.Type == accountType)
            .SumAsync(x => x.CurrentBalance);
    }

    private async Task<MonthlyActivity> GetMonthlyActivityAsync(DateRange dateRange)
    {
        var totals = await TransactionsInDateRange(dateRange)
            .AsNoTracking()
            .GroupBy(_ => 1)
            .Select(x => new
            {
                Income = x.Where(t => t.Amount < 0).Sum(t => -t.Amount),
                Spending = x.Where(t => t.Amount > 0).Sum(t => t.Amount)
            })
            .FirstOrDefaultAsync();

        return new MonthlyActivity(
            totals?.Income ?? 0,
            totals?.Spending ?? 0);
    }

    private async Task<IReadOnlyList<TransactionDto>> GetRecentTransactionsAsync()
    {
        return await _dbContext.Transactions
            .AsNoTracking()
            .OrderByDescending(x => x.Date)
            .Take(RecentTransactionCount)
            .Select(TransactionDtoProjection)
            .ToListAsync();
    }

    private async Task<IReadOnlyList<SpendingByCategoryDto>> GetSpendingByCategoryAsync(DateRange dateRange)
    {
        return await TransactionsInDateRange(dateRange)
            .AsNoTracking()
            .Where(x => x.Amount > 0)
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

    private IQueryable<Transaction> TransactionsInDateRange(DateRange dateRange)
    {
        return _dbContext.Transactions
            .Where(x => x.Date >= dateRange.Start && x.Date <= dateRange.End);
    }

    private static readonly Expression<Func<Transaction, TransactionDto>> TransactionDtoProjection = x => new TransactionDto
    {
        Id = x.Id,
        Date = x.Date,
        AuthorizedDate = x.AuthorizedDate,
        Name = x.Name,
        MerchantName = x.MerchantName,
        Amount = x.Amount,
        IsoCurrencyCode = x.IsoCurrencyCode,
        Pending = x.Pending,
        Account = new TransactionAccountDto
        {
            Id = x.Account.Id,
            Name = x.Account.Name,
            Type = x.Account.Type,
            Subtype = x.Account.Subtype
        },
        Category = x.Category == null
            ? null
            : new TransactionCategoryDto
            {
                Id = x.Category.Id,
                Name = x.Category.Name,
                Color = x.Category.Color,
                Icon = x.Category.Icon
            }
    };

    private sealed record MonthlyActivity(decimal Income, decimal Spending);

    private sealed record DateRange(DateOnly Start, DateOnly End)
    {
        public static DateRange CurrentMonth()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var monthStart = new DateOnly(today.Year, today.Month, 1);

            return new DateRange(monthStart, today);
        }
    }
}
