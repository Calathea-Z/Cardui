using Cardui.Api.Data;
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

    public async Task<IReadOnlyList<AccountDto>> GetAccountsAsync()
    {
        return await _dbContext.Accounts
            .OrderBy(x => x.Name)
            .Select(x => new AccountDto
            {
                Id = x.Id,
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
            .ToListAsync();
    }

    public async Task<AccountSummaryDto> GetAccountsSummaryAsync()
    {
        var accounts = await _dbContext.Accounts
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new AccountDto
            {
                Id = x.Id,
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
            .ToListAsync();

        var groups = new List<AccountGroupDto>
        {
            CreateGroup("net-worth", "Net Worth", accounts),
            CreateGroup("cash", "Cash", accounts),
            CreateGroup("investments", "Investments", accounts),
            CreateGroup("credit-cards", "Credit Cards", accounts),
            CreateGroup("loans", "Loans", accounts)
        };

        var history = await GetBalanceHistoryAsync();

        return new AccountSummaryDto
        {
            NetWorth = groups.Single(x => x.Key == "net-worth").Total,
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
            "net-worth" => accounts,
            "cash" => accounts.Where(IsCashAccount).ToList(),
            "investments" => accounts.Where(IsInvestmentAccount).ToList(),
            "credit-cards" => accounts.Where(IsCreditCardAccount).ToList(),
            "loans" => accounts.Where(IsLoanAccount).ToList(),
            _ => []
        };

        var total = key == "net-worth"
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

    private async Task<IReadOnlyList<AccountBalanceHistoryPointDto>> GetBalanceHistoryAsync()
    {
        var snapshots = await _dbContext.AccountBalanceSnapshots
            .AsNoTracking()
            .Include(x => x.Account)
            .Where(x => x.Account.IsActive)
            .OrderBy(x => x.Date)
            .ToListAsync();

        return snapshots
            .GroupBy(x => x.Date)
            .Select(group =>
            {
                var cash = group
                    .Where(x => IsCashType(x.Account.Type))
                    .Sum(x => x.CurrentBalance);

                var investments = group
                    .Where(x => IsInvestmentType(x.Account.Type))
                    .Sum(x => x.CurrentBalance);

                var creditCards = group
                    .Where(x => IsCreditCardType(x.Account.Type))
                    .Sum(x => x.CurrentBalance);

                var loans = group
                    .Where(x => IsLoanType(x.Account.Type))
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
        var cash = accounts.Where(IsCashAccount).Sum(x => x.CurrentBalance);
        var investments = accounts.Where(IsInvestmentAccount).Sum(x => x.CurrentBalance);
        var creditCards = accounts.Where(IsCreditCardAccount).Sum(x => x.CurrentBalance);
        var loans = accounts.Where(IsLoanAccount).Sum(x => x.CurrentBalance);

        return cash + investments - creditCards - loans;
    }

    private static bool IsCashAccount(AccountDto account)
    {
        return IsCashType(account.Type);
    }

    private static bool IsInvestmentAccount(AccountDto account)
    {
        return IsInvestmentType(account.Type);
    }

    private static bool IsCreditCardAccount(AccountDto account)
    {
        return IsCreditCardType(account.Type);
    }

    private static bool IsLoanAccount(AccountDto account)
    {
        return IsLoanType(account.Type);
    }

    private static bool IsCashType(string type)
    {
        return type.Equals("depository", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsInvestmentType(string type)
    {
        return type.Equals("investment", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCreditCardType(string type)
    {
        return type.Equals("credit", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsLoanType(string type)
    {
        return type.Equals("loan", StringComparison.OrdinalIgnoreCase);
    }
}