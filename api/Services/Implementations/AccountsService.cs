using Cardui.Api.Data;
using Cardui.Api.Domain;
using Cardui.Api.Dtos.Account;
using Cardui.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cardui.Api.Services.Implementations;

public class AccountsService : IAccountsService
{
    private readonly CarduiDBContext _dbContext;

    public AccountsService(CarduiDBContext dbContext)
    {
        _dbContext = dbContext;
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

        var groups = new List<AccountGroupDto>
        {
            CreateGroup(AccountGroupKeys.NetWorth, "Net Worth", accounts),
            CreateGroup(AccountGroupKeys.Cash, "Cash", accounts),
            CreateGroup(AccountGroupKeys.Investments, "Investments", accounts),
            CreateGroup(AccountGroupKeys.CreditCards, "Credit Cards", accounts),
            CreateGroup(AccountGroupKeys.Loans, "Loans", accounts)
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
        IReadOnlyList<AccountDto> accounts)
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

        var total = key == AccountGroupKeys.NetWorth
            ? CalculateNetWorth(accounts)
            : groupAccounts.Sum(x => x.CurrentBalance);

        return new AccountGroupDto
        {
            Key = key,
            Name = name,
            Total = total,
            Accounts = groupAccounts
        };
    }

    private async Task<IReadOnlyList<AccountBalanceHistoryPointDto>> GetBalanceHistoryAsync(
        CancellationToken cancellationToken)
    {
        var snapshots = await _dbContext.AccountBalanceSnapshots
            .AsNoTracking()
            .Include(x => x.Account)
            .Where(x => x.Account.IsActive)
            .OrderBy(x => x.Date)
            .ToListAsync(cancellationToken);

        return snapshots
            .GroupBy(x => x.Date)
            .Select(group =>
            {
                var cash = group
                    .Where(x => AccountTypes.IsCash(x.Account.Type))
                    .Sum(x => x.CurrentBalance);

                var investments = group
                    .Where(x => AccountTypes.IsInvestment(x.Account.Type))
                    .Sum(x => x.CurrentBalance);

                var creditCards = group
                    .Where(x => AccountTypes.IsCreditCard(x.Account.Type))
                    .Sum(x => x.CurrentBalance);

                var loans = group
                    .Where(x => AccountTypes.IsLoan(x.Account.Type))
                    .Sum(x => x.CurrentBalance);

                return new AccountBalanceHistoryPointDto
                {
                    Date = group.Key,
                    Cash = cash,
                    Investments = investments,
                    CreditCards = creditCards,
                    Loans = loans,
                    NetWorth = cash + investments - creditCards - loans
                };
            })
            .OrderBy(x => x.Date)
            .ToList();
    }

    private static decimal CalculateNetWorth(IReadOnlyList<AccountDto> accounts)
    {
        var cash = accounts.Where(x => AccountTypes.IsCash(x.Type)).Sum(x => x.CurrentBalance);
        var investments = accounts.Where(x => AccountTypes.IsInvestment(x.Type)).Sum(x => x.CurrentBalance);
        var creditCards = accounts.Where(x => AccountTypes.IsCreditCard(x.Type)).Sum(x => x.CurrentBalance);
        var loans = accounts.Where(x => AccountTypes.IsLoan(x.Type)).Sum(x => x.CurrentBalance);

        return cash + investments - creditCards - loans;
    }
}
