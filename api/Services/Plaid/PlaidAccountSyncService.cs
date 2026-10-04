using Cardui.Api.Data;
using Cardui.Api.Domain;
using Cardui.Api.Models;
using Cardui.Api.Security;
using Going.Plaid;
using Going.Plaid.Accounts;
using Going.Plaid.Entity;
using Microsoft.EntityFrameworkCore;
using Account = Cardui.Api.Models.Account;
using PlaidAccount = Going.Plaid.Entity.Account;

namespace Cardui.Api.Services.Plaid;

public class PlaidAccountSyncService : IPlaidAccountSyncService
{
    private readonly CarduiDBContext _dbContext;
    private readonly PlaidClient _plaidClient;
    private readonly IPlaidRequestExecutor _requestExecutor;
    private readonly IPlaidAccessTokenProtector _accessTokenProtector;
    private readonly ILogger<PlaidAccountSyncService> _logger;
    private readonly TimeProvider _timeProvider;

    public PlaidAccountSyncService(
        CarduiDBContext dbContext,
        PlaidClient plaidClient,
        IPlaidRequestExecutor requestExecutor,
        IPlaidAccessTokenProtector accessTokenProtector,
        ILogger<PlaidAccountSyncService> logger,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _plaidClient = plaidClient;
        _requestExecutor = requestExecutor;
        _accessTokenProtector = accessTokenProtector;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async Task SyncAccountsForPlaidItemAsync(
        PlaidItem plaidItem,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var response = await FetchAccountsAsync(plaidItem);
        var now = _timeProvider.GetUtcNow();
        var today = FinancialDate.Today(
            _timeProvider,
            await GetHouseholdTimeZoneIdAsync(plaidItem, cancellationToken));
        var responseAccountIds = response.Accounts
            .Select(account => account.AccountId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.Ordinal);

        var itemAccounts = await LoadItemAccountsAsync(plaidItem.Id, cancellationToken);
        var existingSnapshots = await LoadTodaySnapshotsAsync(
            itemAccounts.Values,
            today,
            cancellationToken);
        var processedAccounts = ApplyReturnedAccounts(
            plaidItem,
            response.Accounts,
            itemAccounts,
            now);

        DeactivateAccountsMissingFromBank(
            itemAccounts.Values,
            responseAccountIds,
            plaidItem.Id,
            now);

        foreach (var account in processedAccounts)
        {
            UpsertAccountBalanceSnapshot(account, today, now, existingSnapshots);
        }

        plaidItem.UpdatedAt = now;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    #region Private Methods

    /// <summary>
    /// Asks Plaid for the accounts on this item.
    /// </summary>
    private async Task<AccountsGetResponse> FetchAccountsAsync(PlaidItem plaidItem)
    {
        var accessToken = _accessTokenProtector.Unprotect(plaidItem.AccessToken);
        var request = _requestExecutor.WithCredentials(
            new AccountsGetRequest(),
            accessToken);

        return await _requestExecutor.ExecuteAsync(
            () => _plaidClient.AccountsGetAsync(request));
    }

    /// <summary>
    /// Loads the accounts already stored for this Plaid item.
    /// </summary>
    private async Task<Dictionary<string, Account>> LoadItemAccountsAsync(
        Guid plaidItemId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Accounts
            .Where(account => account.PlaidItemId == plaidItemId && account.PlaidAccountId != null)
            .ToDictionaryAsync(account => account.PlaidAccountId!, cancellationToken);
    }

    /// <summary>
    /// Loads today's balance snapshots for the accounts that already exist.
    /// </summary>
    private async Task<Dictionary<Guid, AccountBalanceSnapshot>> LoadTodaySnapshotsAsync(
        IEnumerable<Account> accounts,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        var accountIds = accounts.Select(account => account.Id).ToList();
        if (accountIds.Count == 0)
        {
            return new Dictionary<Guid, AccountBalanceSnapshot>();
        }

        return await _dbContext.AccountBalanceSnapshots
            .Where(snapshot => accountIds.Contains(snapshot.AccountId) && snapshot.Date == today)
            .ToDictionaryAsync(snapshot => snapshot.AccountId, cancellationToken);
    }

    /// <summary>
    /// Inserts or updates each account Plaid returned.
    /// </summary>
    private List<Account> ApplyReturnedAccounts(
        PlaidItem plaidItem,
        IEnumerable<PlaidAccount> plaidAccounts,
        IDictionary<string, Account> itemAccounts,
        DateTimeOffset now)
    {
        var processedAccounts = new List<Account>();

        foreach (var plaidAccount in plaidAccounts)
        {
            if (string.IsNullOrWhiteSpace(plaidAccount.AccountId))
            {
                continue;
            }

            if (!itemAccounts.TryGetValue(plaidAccount.AccountId, out var account))
            {
                account = CreateAccountFromPlaid(plaidItem, plaidAccount, now);
                _dbContext.Accounts.Add(account);
                itemAccounts[plaidAccount.AccountId] = account;
            }
            else
            {
                account.HouseholdId = plaidItem.HouseholdId;
                ApplyPlaidAccountFields(account, plaidAccount, now);
            }

            processedAccounts.Add(account);
        }

        return processedAccounts;
    }

    /// <summary>
    /// Marks a stored account inactive when the bank no longer returns it.
    /// </summary>
    private void DeactivateAccountsMissingFromBank(
        IEnumerable<Account> storedAccounts,
        IReadOnlySet<string> responseAccountIds,
        Guid plaidItemId,
        DateTimeOffset now)
    {
        foreach (var account in storedAccounts)
        {
            if (account.PlaidAccountId is not string plaidAccountId
                || responseAccountIds.Contains(plaidAccountId)
                || !account.IsActive)
            {
                continue;
            }

            account.IsActive = false;
            account.UpdatedAt = now;

            _logger.LogInformation(
                "Deactivated orphaned account {AccountId} (PlaidAccountId: {PlaidAccountId}) for Plaid item {PlaidItemId}",
                account.Id,
                plaidAccountId,
                plaidItemId);
        }
    }

    /// <summary>
    /// Creates a linked account from a Plaid account and copies the bank fields.
    /// </summary>
    private static Account CreateAccountFromPlaid(
        PlaidItem plaidItem,
        PlaidAccount plaidAccount,
        DateTimeOffset now)
    {
        var account = new Account
        {
            Id = Guid.NewGuid(),
            HouseholdId = plaidItem.HouseholdId,
            PlaidItemId = plaidItem.Id,
            PlaidAccountId = plaidAccount.AccountId,
            Source = FinancialRecordSource.Plaid,
            Provenance = FinancialRecordProvenance.PlaidSync,
            Name = plaidAccount.Name,
            Type = plaidAccount.Type.ToString().ToLowerInvariant(),
            CreatedAt = now,
            UpdatedAt = now
        };

        ApplyPlaidAccountFields(account, plaidAccount, now);
        return account;
    }

    /// <summary>
    /// Copies the name, type, mask, and balances supplied by the bank.
    /// ArchivedAt is left unchanged.
    /// </summary>
    private static void ApplyPlaidAccountFields(
        Account account,
        PlaidAccount plaidAccount,
        DateTimeOffset now)
    {
        account.Name = plaidAccount.Name;
        account.OfficialName = plaidAccount.OfficialName;
        account.Type = plaidAccount.Type.ToString().ToLowerInvariant();
        account.Subtype = plaidAccount.Subtype?.ToString().ToLowerInvariant();
        account.Mask = plaidAccount.Mask;
        account.CurrentBalance = plaidAccount.Balances.Current.HasValue
            ? Convert.ToDecimal(plaidAccount.Balances.Current.Value)
            : 0m;
        account.AvailableBalance = plaidAccount.Balances.Available.HasValue
            ? Convert.ToDecimal(plaidAccount.Balances.Available.Value)
            : null;
        account.IsoCurrencyCode = plaidAccount.Balances.IsoCurrencyCode;
        account.IsActive = true;
        account.UpdatedAt = now;
    }

    /// <summary>
    /// Replaces today's snapshot for the account so the chart uses the bank balance.
    /// </summary>
    private void UpsertAccountBalanceSnapshot(
        Account account,
        DateOnly today,
        DateTimeOffset now,
        IDictionary<Guid, AccountBalanceSnapshot> existingSnapshotsByAccountId)
    {
        if (existingSnapshotsByAccountId.Remove(account.Id, out var existingSnapshot))
        {
            _dbContext.AccountBalanceSnapshots.Remove(existingSnapshot);
        }

        var snapshot = new AccountBalanceSnapshot
        {
            Id = Guid.NewGuid(),
            AccountId = account.Id,
            Date = today,
            CurrentBalance = account.CurrentBalance,
            AvailableBalance = account.AvailableBalance,
            IsoCurrencyCode = account.IsoCurrencyCode,
            CreatedAt = now
        };

        _dbContext.AccountBalanceSnapshots.Add(snapshot);
        existingSnapshotsByAccountId[account.Id] = snapshot;
    }

    /// <summary>
    /// Loads the household time zone used to choose the snapshot date.
    /// An item with no household uses the default time zone.
    /// </summary>
    private async Task<string> GetHouseholdTimeZoneIdAsync(
        PlaidItem plaidItem,
        CancellationToken cancellationToken)
    {
        if (plaidItem.HouseholdId is not Guid householdId)
        {
            return HouseholdTime.DefaultTimeZoneId;
        }

        var timeZoneId = await _dbContext.Households
            .AsNoTracking()
            .Where(x => x.Id == householdId)
            .Select(x => x.TimeZoneId)
            .SingleOrDefaultAsync(cancellationToken);

        return string.IsNullOrWhiteSpace(timeZoneId)
            ? HouseholdTime.DefaultTimeZoneId
            : timeZoneId;
    }

    #endregion
}
