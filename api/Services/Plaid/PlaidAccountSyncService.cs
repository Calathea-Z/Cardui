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
        var accessToken = _accessTokenProtector.Unprotect(plaidItem.AccessToken);

        var request = _requestExecutor.WithCredentials(
            new AccountsGetRequest(),
            accessToken);

        var response = await _requestExecutor.ExecuteAsync(
            () => _plaidClient.AccountsGetAsync(request));

        var now = _timeProvider.GetUtcNow();
        var today = FinancialDate.Today(_timeProvider);

        var responseAccountIds = response.Accounts
            .Select(x => x.AccountId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.Ordinal);

        var itemAccounts = await _dbContext.Accounts
            .Where(x => x.PlaidItemId == plaidItem.Id && x.PlaidAccountId != null)
            .ToDictionaryAsync(x => x.PlaidAccountId!, cancellationToken);

        var accountIds = itemAccounts.Values.Select(x => x.Id).ToList();
        var existingSnapshots = accountIds.Count == 0
            ? new Dictionary<Guid, AccountBalanceSnapshot>()
            : await _dbContext.AccountBalanceSnapshots
                .Where(x => accountIds.Contains(x.AccountId) && x.Date == today)
                .ToDictionaryAsync(x => x.AccountId, cancellationToken);

        var processedAccounts = new List<Account>();

        foreach (var plaidAccount in response.Accounts)
        {
            if (string.IsNullOrWhiteSpace(plaidAccount.AccountId)) continue;

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

        foreach (var account in itemAccounts.Values)
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
                plaidItem.Id);
        }

        foreach (var account in processedAccounts)
        {
            UpsertAccountBalanceSnapshot(account, today, now, existingSnapshots);
        }

        plaidItem.UpdatedAt = now;

        await _dbContext.SaveChangesAsync(cancellationToken);
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
}
