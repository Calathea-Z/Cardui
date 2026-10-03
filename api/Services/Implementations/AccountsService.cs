using Cardui.Api.Data;
using Cardui.Api.Domain;
using Cardui.Api.Dtos.Account;
using Cardui.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cardui.Api.Services.Implementations;

public class AccountsService : IAccountsService
{
    private readonly CarduiDBContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public AccountsService(CarduiDBContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<AccountDto>> GetAccountsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Accounts
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new AccountDto
            {
                Id = x.Id,
                PlaidItemId = x.PlaidItemId,
                Name = x.Name,
                OfficialName = x.OfficialName,
                Type = x.Type,
                Subtype = x.Subtype,
                Mask = x.Mask,
                CurrentBalance = x.CurrentBalance,
                AvailableBalance = x.AvailableBalance,
                IsoCurrencyCode = x.IsoCurrencyCode,
                IsActive = x.IsActive
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<AccountSummaryDto> GetAccountsSummaryAsync(
        CancellationToken cancellationToken = default)
    {
        var accounts = await _dbContext.Accounts
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new AccountDto
            {
                Id = x.Id,
                PlaidItemId = x.PlaidItemId,
                Name = x.Name,
                OfficialName = x.OfficialName,
                Type = x.Type,
                Subtype = x.Subtype,
                Mask = x.Mask,
                CurrentBalance = x.CurrentBalance,
                AvailableBalance = x.AvailableBalance,
                IsoCurrencyCode = x.IsoCurrencyCode,
                IsActive = x.IsActive
            })
            .ToListAsync(cancellationToken);

        var accountTotals = CalculateAccountTotals(accounts);
        var groups = new List<AccountGroupDto>
        {
            CreateGroup(
                AccountGroupKeys.NetWorth,
                "Net Worth",
                accounts,
                accountTotals.NetWorth),
            CreateGroup(AccountGroupKeys.Cash, "Cash", accounts, accountTotals.Cash),
            CreateGroup(
                AccountGroupKeys.Investments,
                "Investments",
                accounts,
                accountTotals.Investments),
            CreateGroup(
                AccountGroupKeys.CreditCards,
                "Credit Cards",
                accounts,
                accountTotals.CreditCards),
            CreateGroup(AccountGroupKeys.Loans, "Loans", accounts, accountTotals.Loans)
        };

        var history = await GetBalanceHistoryAsync(cancellationToken);

        return new AccountSummaryDto
        {
            NetWorth = groups.Single(x => x.Key == AccountGroupKeys.NetWorth).Total,
            Groups = groups,
            History = history
        };
    }

    private static AccountGroupDto CreateGroup(
        string key,
        string name,
        IReadOnlyList<AccountDto> accounts,
        decimal? totalOverride = null)
    {
        var groupAccounts = key switch
        {
            AccountGroupKeys.NetWorth => accounts,
            AccountGroupKeys.Cash => accounts.Where(x => AccountTypes.IsCash(x.Type)).ToList(),
            AccountGroupKeys.Investments => accounts.Where(x => AccountTypes.IsInvestment(x.Type)).ToList(),
            AccountGroupKeys.CreditCards => accounts.Where(x => AccountTypes.IsCreditCard(x.Type)).ToList(),
            AccountGroupKeys.Loans => accounts.Where(x => AccountTypes.IsLoan(x.Type)).ToList(),
            _ => []
        };

        return new AccountGroupDto
        {
            Key = key,
            Name = name,
            Total = totalOverride ?? groupAccounts.Sum(x => x.CurrentBalance),
            Accounts = groupAccounts
        };
    }

    private async Task<IReadOnlyList<AccountBalanceHistoryPointDto>> GetBalanceHistoryAsync(
        CancellationToken cancellationToken)
    {
        var today = FinancialDate.Today(_timeProvider);
        var snapshots = await _dbContext.AccountBalanceSnapshots
            .AsNoTracking()
            .Include(x => x.Account)
            .Where(x => x.Account.IsActive && x.Date <= today)
            .OrderBy(x => x.Date)
            .ToListAsync(cancellationToken);

        return snapshots
            .GroupBy(x => x.Date)
            .Select(group =>
            {
                var totals = AccountTotalsCalculator.Calculate(
                    group.Select(x => new AccountBalanceValue(
                        x.Account.Type,
                        x.CurrentBalance)));

                return new AccountBalanceHistoryPointDto
                {
                    Date = group.Key,
                    Cash = totals.Cash,
                    Investments = totals.Investments,
                    CreditCards = totals.CreditCards,
                    Loans = totals.Loans,
                    NetWorth = totals.NetWorth
                };
            })
            .OrderBy(x => x.Date)
            .ToList();
    }

    private static AccountTotals CalculateAccountTotals(IReadOnlyList<AccountDto> accounts)
    {
        return AccountTotalsCalculator.Calculate(
            accounts.Select(x => new AccountBalanceValue(x.Type, x.CurrentBalance)));
    }
}
