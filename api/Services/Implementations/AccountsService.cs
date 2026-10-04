using Cardui.Api.Data;
using Cardui.Api.Domain;
using Cardui.Api.Dtos.Account;
using Cardui.Api.Exceptions;
using Cardui.Api.Mapping;
using Cardui.Api.Models;
using Cardui.Api.Security;
using Cardui.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cardui.Api.Services.Implementations;

public class AccountsService : IAccountsService
{
    private readonly CarduiDBContext _dbContext;
    private readonly TimeProvider _timeProvider;
    private readonly HouseholdScope _householdScope;

    public AccountsService(
        CarduiDBContext dbContext,
        TimeProvider timeProvider,
        HouseholdScope householdScope)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
        _householdScope = householdScope;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AccountDto>> GetAccountsAsync(
        bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        var accounts = _dbContext.Accounts
            .AsNoTracking()
            .InHousehold(_householdScope);

        if (!includeArchived)
        {
            accounts = accounts.Where(x => x.ArchivedAt == null);
        }

        var result = await accounts
            .OrderBy(x => x.Name)
            .Select(AccountDtoMapper.Projection)
            .ToListAsync(cancellationToken);
        MarkPlanningTotals(result, _householdScope.PlanningCurrency);
        return result;
    }

    /// <inheritdoc />
    public async Task<AccountSummaryDto> GetAccountsSummaryAsync(
        CancellationToken cancellationToken = default)
    {
        var accounts = await _dbContext.Accounts
            .AsNoTracking()
            .InHousehold(_householdScope)
            .Where(x => x.IsActive && x.ArchivedAt == null)
            .OrderBy(x => x.Name)
            .Select(AccountDtoMapper.Projection)
            .ToListAsync(cancellationToken);

        var archivedAccounts = await _dbContext.Accounts
            .AsNoTracking()
            .InHousehold(_householdScope)
            .Where(x => x.ArchivedAt != null)
            .OrderBy(x => x.Name)
            .Select(AccountDtoMapper.Projection)
            .ToListAsync(cancellationToken);

        var planningCurrency = _householdScope.PlanningCurrency;
        MarkPlanningTotals(accounts, planningCurrency);
        MarkPlanningTotals(archivedAccounts, planningCurrency);
        var excludedAccounts = accounts
            .Where(x => !x.CountsInPlanningTotals)
            .ToList();
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
            PlanningCurrency = planningCurrency,
            ExcludedAccountCount = excludedAccounts.Count,
            ExcludedCurrencies = PlanningCurrencyRules.ExcludedCodes(
                excludedAccounts.Select(x => x.IsoCurrencyCode),
                planningCurrency),
            Groups = groups,
            History = history,
            ArchivedAccounts = archivedAccounts
        };
    }

    /// <inheritdoc />
    public async Task<AccountDto> CreateManualAccountAsync(
        CreateManualAccountDto dto,
        CancellationToken cancellationToken = default)
    {
        var householdId = _householdScope.RequireHouseholdId();
        var today = Today();
        var now = _timeProvider.GetUtcNow();
        var openingDate = RequireOpeningDate(dto.OpeningBalanceDate, today);

        var account = new Account
        {
            Id = Guid.NewGuid(),
            HouseholdId = householdId,
            PlaidItemId = null,
            PlaidAccountId = null,
            Source = FinancialRecordSource.Manual,
            Provenance = FinancialRecordProvenance.ManualEntry,
            Name = RequireName(dto.Name, 200),
            Type = RequireAccountType(dto.Type),
            Subtype = EmptyToNull(dto.Subtype),
            Mask = EmptyToNull(dto.Mask),
            CurrentBalance = AccountLedger.Round(dto.OpeningBalance),
            AvailableBalance = null,
            IsoCurrencyCode = NormalizeCurrency(dto.IsoCurrencyCode),
            OpeningBalance = AccountLedger.Round(dto.OpeningBalance),
            OpeningBalanceDate = openingDate,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.Accounts.Add(account);
        await ManualAccountBalance.UpsertSnapshotAsync(
            _dbContext,
            account,
            today,
            now,
            cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await ProjectAccountAsync(account.Id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AccountDto> UpdateManualAccountAsync(
        Guid accountId,
        UpdateManualAccountDto dto,
        CancellationToken cancellationToken = default)
    {
        var account = await FindAccountAsync(accountId, cancellationToken);
        if (account.PlaidItemId is not null)
        {
            throw new BadRequestException(
                "Linked accounts keep their name, type, and balance from the bank. You can archive the account.");
        }

        var today = Today();
        var openingDate = RequireOpeningDate(dto.OpeningBalanceDate, today);
        await RequireOpeningDateCoversTransactionsAsync(
            account.Id,
            openingDate,
            cancellationToken);

        account.Name = RequireName(dto.Name, 200);
        account.Type = RequireAccountType(dto.Type);
        account.Subtype = EmptyToNull(dto.Subtype);
        account.Mask = EmptyToNull(dto.Mask);
        account.IsoCurrencyCode = NormalizeCurrency(dto.IsoCurrencyCode);
        account.OpeningBalance = AccountLedger.Round(dto.OpeningBalance);
        account.OpeningBalanceDate = openingDate;
        account.Source = FinancialRecordSource.Manual;
        account.Provenance = FinancialRecordProvenance.ManualEntry;

        await ManualAccountBalance.RefreshAsync(
            _dbContext,
            account,
            today,
            _timeProvider.GetUtcNow(),
            cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await ProjectAccountAsync(account.Id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AccountDto> ArchiveAccountAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        var account = await FindAccountAsync(accountId, cancellationToken);
        account.ArchivedAt ??= _timeProvider.GetUtcNow();
        account.UpdatedAt = _timeProvider.GetUtcNow();
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await ProjectAccountAsync(account.Id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AccountDto> RestoreAccountAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        var account = await FindAccountAsync(accountId, cancellationToken);
        account.ArchivedAt = null;
        account.UpdatedAt = _timeProvider.GetUtcNow();
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await ProjectAccountAsync(account.Id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<BalanceReconciliationResultDto> ReconcileBalanceAsync(
        Guid accountId,
        ReconcileAccountBalanceDto dto,
        CancellationToken cancellationToken = default)
    {
        var account = await FindAccountAsync(accountId, cancellationToken);
        if (!ManualAccountBalance.UsesLedger(account)
            || account.OpeningBalanceDate is not DateOnly openingDate)
        {
            throw new BadRequestException(
                "Balance reconciliation is for accounts you enter yourself. Linked balances come from the bank.");
        }

        if (account.ArchivedAt is not null)
        {
            throw new BadRequestException("Restore this account before reconciling its balance.");
        }

        var today = Today();
        if (dto.AsOfDate < openingDate || dto.AsOfDate > today)
        {
            throw new BadRequestException(
                "The statement date must be on or after the opening date, and not in the future.");
        }

        var now = _timeProvider.GetUtcNow();
        var transactions = await _dbContext.Transactions
            .AsNoTracking()
            .Where(x => x.AccountId == account.Id)
            .Select(x => new LedgerTransaction(
                x.Date,
                x.Amount,
                x.Pending,
                x.ArchivedAt != null))
            .ToListAsync(cancellationToken);

        var calculated = AccountLedger.BalanceAsOf(
            account.Type,
            account.OpeningBalance,
            openingDate,
            transactions,
            dto.AsOfDate);
        var statementBalance = AccountLedger.Round(dto.StatementBalance);
        var adjustment = AccountLedger.Round(statementBalance - calculated);
        Guid? adjustmentTransactionId = null;

        if (adjustment != 0)
        {
            var transaction = CreateAdjustmentTransaction(
                account,
                dto.AsOfDate,
                adjustment,
                now);
            _dbContext.Transactions.Add(transaction);
            adjustmentTransactionId = transaction.Id;
        }

        await ManualAccountBalance.RefreshAsync(
            _dbContext,
            account,
            today,
            now,
            cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new BalanceReconciliationResultDto
        {
            Account = await ProjectAccountAsync(account.Id, cancellationToken),
            CalculatedBalance = calculated,
            StatementBalance = statementBalance,
            Adjustment = adjustment,
            AdjustmentTransactionId = adjustmentTransactionId
        };
    }

    #region Private Methods

    /// <summary>
    /// Builds the unsaved transaction that moves the ledger to the statement balance.
    /// </summary>
    private static Transaction CreateAdjustmentTransaction(
        Account account,
        DateOnly asOfDate,
        decimal adjustment,
        DateTimeOffset now)
    {
        return new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = account.Id,
            PlaidTransactionId = null,
            Source = FinancialRecordSource.Manual,
            Provenance = FinancialRecordProvenance.BalanceReconciliation,
            Date = asOfDate,
            Name = BalanceReconciliation.Name,
            MerchantName = BalanceReconciliation.Name,
            Amount = AccountLedger.TransactionAmountForBalanceChange(
                account.Type,
                adjustment),
            IsoCurrencyCode = account.IsoCurrencyCode,
            Pending = false,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    /// <summary>
    /// Builds one summary group. Net worth includes every active account.
    /// The other groups keep only accounts of that type.
    /// </summary>
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

    /// <summary>
    /// Builds daily balance history from the snapshot columns the chart needs.
    /// Snapshots in another currency are left out. Each account keeps its last known balance.
    /// </summary>
    private async Task<IReadOnlyList<AccountBalanceHistoryPointDto>> GetBalanceHistoryAsync(
        CancellationToken cancellationToken)
    {
        var today = Today();
        var planningCurrency = _householdScope.PlanningCurrency;
        var snapshots = await _dbContext.AccountBalanceSnapshots
            .AsNoTracking()
            .InHousehold(_dbContext, _householdScope)
            .Where(x => x.Account.IsActive && x.Account.ArchivedAt == null && x.Date <= today)
            .OrderBy(x => x.Date)
            .Select(x => new
            {
                x.AccountId,
                x.Account.Type,
                x.Date,
                x.CurrentBalance,
                x.CreatedAt,
                x.Account.IsoCurrencyCode
            })
            .ToListAsync(cancellationToken);

        var includedSnapshots = snapshots.Where(x =>
            PlanningCurrencyRules.IsIncluded(x.IsoCurrencyCode, planningCurrency));

        return AccountBalanceHistory.Build(includedSnapshots.Select(x => new AccountSnapshotBalance(
                x.AccountId,
                x.Type,
                x.Date,
                x.CurrentBalance,
                x.CreatedAt)))
            .Select(point => new AccountBalanceHistoryPointDto
            {
                Date = point.Date,
                Cash = point.Cash,
                Investments = point.Investments,
                CreditCards = point.CreditCards,
                Loans = point.Loans,
                NetWorth = point.NetWorth
            })
            .ToList();
    }

    /// <summary>
    /// Loads a tracked household account, or throws when it is missing.
    /// </summary>
    private async Task<Account> FindAccountAsync(
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var account = await _dbContext.Accounts
            .InHousehold(_householdScope)
            .FirstOrDefaultAsync(x => x.Id == accountId, cancellationToken);

        if (account is null)
        {
            throw new NotFoundException($"Account '{accountId}' was not found.");
        }

        return account;
    }

    /// <summary>
    /// Loads the API shape of one household account, or throws when it is missing.
    /// </summary>
    private async Task<AccountDto> ProjectAccountAsync(
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var account = await _dbContext.Accounts
            .AsNoTracking()
            .InHousehold(_householdScope)
            .Where(x => x.Id == accountId)
            .Select(AccountDtoMapper.Projection)
            .FirstOrDefaultAsync(cancellationToken);

        if (account is null)
        {
            throw new NotFoundException($"Account '{accountId}' was not found.");
        }

        MarkPlanningTotals([account], _householdScope.PlanningCurrency);
        return account;
    }

    /// <summary>
    /// Today's date in the household time zone.
    /// </summary>
    private DateOnly Today()
    {
        return FinancialDate.Today(_timeProvider, _householdScope.TimeZoneId);
    }

    /// <summary>
    /// Marks each account that counts toward planning totals for this currency.
    /// </summary>
    private static void MarkPlanningTotals(
        IEnumerable<AccountDto> accounts,
        string planningCurrency)
    {
        foreach (var account in accounts)
        {
            account.CountsInPlanningTotals = PlanningCurrencyRules.IsIncluded(
                account.IsoCurrencyCode,
                planningCurrency);
        }
    }

    /// <summary>
    /// Rejects an opening date that would leave existing transactions before it.
    /// </summary>
    private async Task RequireOpeningDateCoversTransactionsAsync(
        Guid accountId,
        DateOnly openingDate,
        CancellationToken cancellationToken)
    {
        var hasEarlierTransaction = await _dbContext.Transactions
            .AnyAsync(
                x => x.AccountId == accountId
                    && x.ArchivedAt == null
                    && x.Date < openingDate,
                cancellationToken);

        if (hasEarlierTransaction)
        {
            throw new BadRequestException(
                "Transactions already exist before that opening date. Move those transactions or pick an earlier date.");
        }
    }

    /// <summary>
    /// Rejects an opening date in the future.
    /// </summary>
    private static DateOnly RequireOpeningDate(DateOnly openingDate, DateOnly today)
    {
        if (openingDate > today)
        {
            throw new BadRequestException("The opening date cannot be in the future.");
        }

        return openingDate;
    }

    /// <summary>
    /// Accepts depository, investment, credit, or loan, and stores the type in lowercase.
    /// </summary>
    private static string RequireAccountType(string type)
    {
        var normalized = type.Trim().ToLowerInvariant();
        if (normalized is not (
            AccountTypes.Depository
            or AccountTypes.Investment
            or AccountTypes.Credit
            or AccountTypes.Loan))
        {
            throw new BadRequestException(
                "Account type must be cash, investment, credit card, or loan.");
        }

        return normalized;
    }

    /// <summary>
    /// Trims a required name and rejects an empty value or one past the maximum length.
    /// </summary>
    private static string RequireName(string name, int maxLength)
    {
        var trimmed = name.Trim();
        if (trimmed.Length == 0 || trimmed.Length > maxLength)
        {
            throw new BadRequestException("A name is required.");
        }

        return trimmed;
    }

    /// <summary>
    /// Trims optional text and stores blank input as null.
    /// </summary>
    private static string? EmptyToNull(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    /// <summary>
    /// Uses the planning currency when no currency is supplied, and rejects a code that is not 3 to 10 characters.
    /// </summary>
    private string NormalizeCurrency(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return _householdScope.PlanningCurrency;
        }

        var currency = value.Trim().ToUpperInvariant();
        if (currency.Length is < 3 or > 10)
        {
            throw new BadRequestException("Enter a currency code such as USD.");
        }

        return currency;
    }

    /// <summary>
    /// Totals the supplied accounts by cash, investment, credit card, and loan.
    /// </summary>
    private static AccountTotals CalculateAccountTotals(IReadOnlyList<AccountDto> accounts)
    {
        return AccountTotalsCalculator.Calculate(
            accounts
                .Where(x => x.CountsInPlanningTotals)
                .Select(x => new AccountBalanceValue(x.Type, x.CurrentBalance)));
    }
    #endregion
}
