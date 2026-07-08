using Cardui.Api.Data;
using Cardui.Api.Dtos.Plaid;
using Cardui.Api.Exceptions;
using Cardui.Api.Models;
using Cardui.Api.Services.Interfaces;
using Going.Plaid;
using Going.Plaid.Accounts;
using Going.Plaid.Entity;
using Going.Plaid.Item;
using Going.Plaid.Link;
using Going.Plaid.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Account = Cardui.Api.Models.Account;
using PlaidConfig = Cardui.Api.Options.PlaidOptions;
using PlaidAccount = Going.Plaid.Entity.Account;
using Transaction = Cardui.Api.Models.Transaction;

namespace Cardui.Api.Services.Implementations;

internal enum TransactionUpsertResult
{
    Skipped,
    Added,
    Modified
}

public class PlaidService : IPlaidService
{
    private readonly CarduiDBContext _dbContext;
    private readonly PlaidClient _plaidClient;
    private readonly PlaidConfig _plaidOptions;
    private readonly ITransactionCategorizationService _transactionCategorizationService;
    private readonly ILogger<PlaidService> _logger;
    private readonly TimeProvider _timeProvider;

    public PlaidService(
        CarduiDBContext dbContext,
        PlaidClient plaidClient,
        IOptions<PlaidConfig> plaidOptions,
        ITransactionCategorizationService transactionCategorizationService,
        ILogger<PlaidService> logger,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _plaidClient = plaidClient;
        _plaidOptions = plaidOptions.Value;
        _transactionCategorizationService = transactionCategorizationService;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task<CreateLinkTokenResponseDto> CreateLinkTokenAsync()
    {
        var request = WithCredentials(new LinkTokenCreateRequest
        {
            ClientName = _plaidOptions.ClientName,
            Language = Language.English,
            CountryCodes = new List<CountryCode>
            {
                CountryCode.Us
            },
            Products = new List<Products>
            {
                Products.Transactions
            },
            User = new LinkTokenCreateRequestUser
            {
                ClientUserId = _plaidOptions.DefaultClientUserId
            }
        });

        var response = await ExecutePlaidRequestAsync(
            () => _plaidClient.LinkTokenCreateAsync(request));

        return new CreateLinkTokenResponseDto
        {
            LinkToken = response.LinkToken
        };
    }

    public async Task<ExchangePublicTokenResponseDto> ExchangePublicTokenAsync(
        ExchangePublicTokenRequestDto dto)
    {
        var request = WithCredentials(new ItemPublicTokenExchangeRequest
        {
            PublicToken = dto.PublicToken
        });

        var response = await ExecutePlaidRequestAsync(
            () => _plaidClient.ItemPublicTokenExchangeAsync(request));

        var now = _timeProvider.GetUtcNow();

        var plaidItem = new PlaidItem
        {
            Id = Guid.NewGuid(),
            PlaidItemId = response.ItemId,
            AccessToken = response.AccessToken,
            InstitutionId = dto.InstitutionId,
            InstitutionName = dto.InstitutionName,
            CreatedAt = now,
            UpdatedAt = now
        };

        await using var dbTransaction = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            _dbContext.PlaidItems.Add(plaidItem);
            await SyncAccountsForPlaidItemAsync(plaidItem);
            await SyncTransactionsForPlaidItemAsync(plaidItem);
            await dbTransaction.CommitAsync();
        }
        catch
        {
            await dbTransaction.RollbackAsync();
            throw;
        }

        return new ExchangePublicTokenResponseDto
        {
            PlaidItemId = plaidItem.Id
        };
    }

    public async Task SyncAccountsAsync(Guid plaidItemId)
    {
        var plaidItem = await GetPlaidItemOrThrowAsync(plaidItemId);
        await SyncAccountsForPlaidItemAsync(plaidItem);
    }

    public async Task<SyncTransactionsResponseDto> SyncTransactionsAsync(Guid plaidItemId)
    {
        var plaidItem = await GetPlaidItemOrThrowAsync(plaidItemId);
        return await SyncTransactionsForPlaidItemAsync(plaidItem);
    }

    public async Task<IReadOnlyList<PlaidItemDto>> GetPlaidItemsAsync()
    {
        return await _dbContext.PlaidItems
            .AsNoTracking()
            .OrderBy(x => x.InstitutionName)
            .Select(x => new PlaidItemDto
            {
                Id = x.Id,
                InstitutionId = x.InstitutionId,
                InstitutionName = x.InstitutionName,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                LastTransactionsSyncedAt = x.LastTransactionsSyncedAt
            })
            .ToListAsync();
    }

    public async Task<SyncPlaidItemResponseDto> SyncPlaidItemAsync(Guid plaidItemId)
    {
        var plaidItem = await GetPlaidItemOrThrowAsync(plaidItemId);
        var now = _timeProvider.GetUtcNow();

        plaidItem.LastSyncStartedAt = now;
        plaidItem.LastSyncCompletedAt = null;
        plaidItem.LastSyncFailedAt = null;
        plaidItem.LastSyncError = null;
        plaidItem.UpdatedAt = now;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Starting Plaid sync for item {PlaidItemId}", plaidItemId);

        try
        {
            await SyncAccountsForPlaidItemAsync(plaidItem);
            var transactionsResult = await SyncTransactionsForPlaidItemAsync(plaidItem);

            plaidItem.LastSyncCompletedAt = _timeProvider.GetUtcNow();
            plaidItem.LastSyncFailedAt = null;
            plaidItem.LastSyncError = null;
            plaidItem.UpdatedAt = _timeProvider.GetUtcNow();

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation(
                "Completed Plaid sync for item {PlaidItemId}. Added {Added}, modified {Modified}, removed {Removed}",
                plaidItemId,
                transactionsResult.Added,
                transactionsResult.Modified,
                transactionsResult.Removed);

            return new SyncPlaidItemResponseDto
            {
                PlaidItemId = plaidItem.Id,
                Transactions = transactionsResult
            };
        }
        catch (Exception ex)
        {
            RecordSyncFailure(plaidItem, ex);
            await _dbContext.SaveChangesAsync();
            throw;
        }
    }

    #region PrivateMethods

    private async Task<PlaidItem> GetPlaidItemOrThrowAsync(
        Guid plaidItemId,
        CancellationToken cancellationToken = default)
    {
        var plaidItem = await _dbContext.PlaidItems
            .FirstOrDefaultAsync(x => x.Id == plaidItemId, cancellationToken);

        return plaidItem ?? throw new NotFoundException($"Plaid item '{plaidItemId}' was not found.");
    }

    private TRequest WithCredentials<TRequest>(TRequest request, string? accessToken = null)
        where TRequest : RequestBase
    {
        request.ClientId = _plaidOptions.ClientId;
        request.Secret = _plaidOptions.Secret;

        if (!string.IsNullOrWhiteSpace(accessToken))
            request.AccessToken = accessToken;

        return request;
    }

    private async Task<TResponse> ExecutePlaidRequestAsync<TResponse>(Func<Task<TResponse>> action)
        where TResponse : ResponseBase
    {
        try
        {
            var response = await action();

            return response.Error is not null ? throw CreatePlaidSyncException(response.Error, response.RequestId) : response;
        }
        catch (PlaidSyncException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Plaid API request failed");
            throw new PlaidSyncException(
                "Unable to reach Plaid. Please try again.",
                innerException: ex);
        }
    }

    private PlaidSyncException CreatePlaidSyncException(PlaidError error, string? requestId)
    {
        _logger.LogError(
            "Plaid API error {ErrorType}/{ErrorCode} (RequestId: {RequestId})",
            error.ErrorType,
            error.ErrorCode,
            error.RequestId ?? requestId);

        return new PlaidSyncException(
            GetUserSafePlaidMessage(error),
            error.ErrorCode,
            error.ErrorType);
    }

    private static string GetUserSafePlaidMessage(PlaidError ex)
    {
        if (!string.IsNullOrWhiteSpace(ex.DisplayMessage))
            return ex.DisplayMessage;

        return !string.IsNullOrWhiteSpace(ex.ErrorMessage) ? ex.ErrorMessage : "Plaid request failed. Please try again.";
    }

    private void RecordSyncFailure(PlaidItem plaidItem, Exception ex)
    {
        _logger.LogError(ex, "Plaid sync failed for item {PlaidItemId}", plaidItem.Id);

        plaidItem.LastSyncFailedAt = _timeProvider.GetUtcNow();
        plaidItem.LastSyncError = FormatSyncError(ex);
        plaidItem.UpdatedAt = _timeProvider.GetUtcNow();
    }

    private static string FormatSyncError(Exception ex)
    {
        if (ex is not PlaidSyncException plaidSyncException) return "Sync failed. Please try again.";
        if (!string.IsNullOrWhiteSpace(plaidSyncException.PlaidErrorCode))
        {
            return string.IsNullOrWhiteSpace(plaidSyncException.PlaidErrorType)
                ? plaidSyncException.PlaidErrorCode
                : $"{plaidSyncException.PlaidErrorType}:{plaidSyncException.PlaidErrorCode}";
        }

        return plaidSyncException.Message;

    }

    private async Task SyncAccountsForPlaidItemAsync(PlaidItem plaidItem)
    {
        var request = WithCredentials(new AccountsGetRequest(), plaidItem.AccessToken);

        var response = await ExecutePlaidRequestAsync(
            () => _plaidClient.AccountsGetAsync(request));

        var now = _timeProvider.GetUtcNow();
        var today = DateOnly.FromDateTime(now.UtcDateTime);

        var responseAccountIds = response.Accounts
            .Select(x => x.AccountId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.Ordinal);

        var itemAccounts = await _dbContext.Accounts
            .Where(x => x.PlaidItemId == plaidItem.Id)
            .ToDictionaryAsync(x => x.PlaidAccountId);

        var accountIds = itemAccounts.Values.Select(x => x.Id).ToList();
        var existingSnapshots = accountIds.Count == 0
            ? new Dictionary<Guid, AccountBalanceSnapshot>()
            : await _dbContext.AccountBalanceSnapshots
                .Where(x => accountIds.Contains(x.AccountId) && x.Date == today)
                .ToDictionaryAsync(x => x.AccountId);

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
                ApplyPlaidAccountFields(account, plaidAccount, now);
            }

            processedAccounts.Add(account);
        }

        foreach (var account in itemAccounts.Values)
        {
            if (responseAccountIds.Contains(account.PlaidAccountId) || !account.IsActive) continue;

            account.IsActive = false;
            account.UpdatedAt = now;

            _logger.LogInformation(
                "Deactivated orphaned account {AccountId} (PlaidAccountId: {PlaidAccountId}) for Plaid item {PlaidItemId}",
                account.Id,
                account.PlaidAccountId,
                plaidItem.Id);
        }

        foreach (var account in processedAccounts)
            UpsertAccountBalanceSnapshot(account, today, now, existingSnapshots);

        plaidItem.UpdatedAt = now;

        await _dbContext.SaveChangesAsync();
    }

    private static Account CreateAccountFromPlaid(
        PlaidItem plaidItem,
        PlaidAccount plaidAccount,
        DateTimeOffset now)
    {
        var account = new Account
        {
            Id = Guid.NewGuid(),
            PlaidItemId = plaidItem.Id,
            PlaidAccountId = plaidAccount.AccountId,
            Name = plaidAccount.Name,
            Type = plaidAccount.Type.ToString().ToLowerInvariant(),
            CreatedAt = now,
            UpdatedAt = now
        };

        ApplyPlaidAccountFields(account, plaidAccount, now);
        return account;
    }

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

    private async Task<TransactionUpsertResult> UpsertPlaidTransactionAsync(
        Going.Plaid.Entity.Transaction plaidTransaction,
        Guid plaidItemId,
        IReadOnlyDictionary<string, Account> accountsByPlaidId,
        IDictionary<string, Transaction> existingTransactionsByPlaidId,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(plaidTransaction.TransactionId))
            throw new InvalidOperationException("Plaid transaction is missing transaction id.");

        if (plaidTransaction.Date is null)
            throw new InvalidOperationException(
                $"Plaid transaction {plaidTransaction.TransactionId} is missing date.");

        if (string.IsNullOrWhiteSpace(plaidTransaction.AccountId)
            || !accountsByPlaidId.TryGetValue(plaidTransaction.AccountId, out var account))
        {
            _logger.LogWarning(
                "Skipping Plaid transaction {TransactionId} because account {PlaidAccountId} was not found for Plaid item {PlaidItemId}",
                plaidTransaction.TransactionId,
                plaidTransaction.AccountId,
                plaidItemId);

            return TransactionUpsertResult.Skipped;
        }

        existingTransactionsByPlaidId.TryGetValue(
            plaidTransaction.TransactionId,
            out var existingTransaction);

        var name = plaidTransaction.MerchantName
                   ?? plaidTransaction.OriginalDescription
                   ?? "Unknown transaction";

        if (existingTransaction is null)
        {
            var categoryId = await _transactionCategorizationService
                .GetCategoryIdForPlaidTransactionAsync(plaidTransaction);

            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                AccountId = account.Id,
                PlaidTransactionId = plaidTransaction.TransactionId,
                Date = plaidTransaction.Date.Value,
                AuthorizedDate = plaidTransaction.AuthorizedDate,
                Name = name,
                MerchantName = plaidTransaction.MerchantName,
                Amount = Convert.ToDecimal(plaidTransaction.Amount),
                IsoCurrencyCode = plaidTransaction.IsoCurrencyCode,
                Pending = plaidTransaction.Pending ?? false,
                CategoryId = categoryId,
                Notes = null,
                CreatedAt = now,
                UpdatedAt = now
            };

            _dbContext.Transactions.Add(transaction);
            existingTransactionsByPlaidId[plaidTransaction.TransactionId] = transaction;
            return TransactionUpsertResult.Added;
        }

        existingTransaction.AccountId = account.Id;
        existingTransaction.Date = plaidTransaction.Date.Value;
        existingTransaction.AuthorizedDate = plaidTransaction.AuthorizedDate;
        existingTransaction.Name = name;
        existingTransaction.MerchantName = plaidTransaction.MerchantName;
        existingTransaction.Amount = Convert.ToDecimal(plaidTransaction.Amount);
        existingTransaction.IsoCurrencyCode = plaidTransaction.IsoCurrencyCode;
        existingTransaction.Pending = plaidTransaction.Pending ?? false;
        existingTransaction.UpdatedAt = now;

        return TransactionUpsertResult.Modified;
    }

    private async Task<SyncTransactionsResponseDto> SyncTransactionsForPlaidItemAsync(
        PlaidItem plaidItem)
    {
        var addedCount = 0;
        var modifiedCount = 0;
        var removedCount = 0;
        var hasMore = true;
        var cursor = plaidItem.TransactionsCursor;

        var accountsByPlaidId = await _dbContext.Accounts
            .Where(x => x.PlaidItemId == plaidItem.Id)
            .ToDictionaryAsync(x => x.PlaidAccountId);

        while (hasMore)
        {
            var request = WithCredentials(new TransactionsSyncRequest
            {
                Cursor = cursor,
                Count = 100,
                Options = new TransactionsSyncRequestOptions
                {
                    IncludeOriginalDescription = true
                }
            }, plaidItem.AccessToken);

            var response = await ExecutePlaidRequestAsync(
                () => _plaidClient.TransactionsSyncAsync(request));

            var now = _timeProvider.GetUtcNow();

            var pageTransactionIds = response.Added
                .Concat(response.Modified)
                .Select(x => x.TransactionId)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            var existingTransactionsByPlaidId = pageTransactionIds.Count == 0
                ? new Dictionary<string, Transaction>()
                : await _dbContext.Transactions
                    .Where(x => pageTransactionIds.Contains(x.PlaidTransactionId))
                    .ToDictionaryAsync(x => x.PlaidTransactionId);

            foreach (var plaidTransaction in response.Added)
            {
                var result = await UpsertPlaidTransactionAsync(
                    plaidTransaction,
                    plaidItem.Id,
                    accountsByPlaidId,
                    existingTransactionsByPlaidId,
                    now);

                switch (result)
                {
                    case TransactionUpsertResult.Added:
                        addedCount++;
                        break;
                    case TransactionUpsertResult.Modified:
                        modifiedCount++;
                        break;
                }
            }

            foreach (var plaidTransaction in response.Modified)
            {
                var result = await UpsertPlaidTransactionAsync(
                    plaidTransaction,
                    plaidItem.Id,
                    accountsByPlaidId,
                    existingTransactionsByPlaidId,
                    now);

                switch (result)
                {
                    case TransactionUpsertResult.Added:
                        addedCount++;
                        break;
                    case TransactionUpsertResult.Modified:
                        modifiedCount++;
                        break;
                }
            }

            var removedTransactionIds = response.Removed
                .Select(x => x.TransactionId)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            if (removedTransactionIds.Count > 0)
            {
                var transactionsToRemove = await _dbContext.Transactions
                    .Where(x => removedTransactionIds.Contains(x.PlaidTransactionId))
                    .ToListAsync();

                foreach (var existingTransaction in transactionsToRemove)
                {
                    _dbContext.Transactions.Remove(existingTransaction);
                    removedCount++;
                }
            }

            cursor = response.NextCursor;
            hasMore = response.HasMore;

            plaidItem.TransactionsCursor = cursor;
            plaidItem.UpdatedAt = now;

            await _dbContext.SaveChangesAsync();
        }

        plaidItem.LastTransactionsSyncedAt = _timeProvider.GetUtcNow();
        plaidItem.UpdatedAt = _timeProvider.GetUtcNow();

        await _dbContext.SaveChangesAsync();

        return new SyncTransactionsResponseDto
        {
            Added = addedCount,
            Modified = modifiedCount,
            Removed = removedCount,
            NextCursor = cursor
        };
    }

    private void UpsertAccountBalanceSnapshot(
        Account account,
        DateOnly today,
        DateTimeOffset now,
        IDictionary<Guid, AccountBalanceSnapshot> existingSnapshotsByAccountId)
    {
        if (existingSnapshotsByAccountId.Remove(account.Id, out var existingSnapshot))
            _dbContext.AccountBalanceSnapshots.Remove(existingSnapshot);

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

    #endregion
}
