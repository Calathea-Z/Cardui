using Cardui.Api.Data;
using Cardui.Api.Domain.Plaid;
using Cardui.Api.Dtos.Plaid;
using Cardui.Api.Exceptions;
using Cardui.Api.Models;
using Cardui.Api.Security;
using Cardui.Api.Services.Interfaces;
using Cardui.Api.Services.Plaid;
using Going.Plaid.Entity;
using Going.Plaid.Item;
using Going.Plaid.Link;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PlaidConfig = Cardui.Api.Options.PlaidOptions;

namespace Cardui.Api.Services.Implementations;

public class PlaidService : IPlaidService
{
    private readonly CarduiDBContext _dbContext;
    private readonly IPlaidClientSource _clientSource;
    private readonly PlaidConfig _plaidOptions;
    private readonly IPlaidRequestExecutor _requestExecutor;
    private readonly IPlaidLinkClient _linkClient;
    private readonly IPlaidAccountSyncService _accountSyncService;
    private readonly IPlaidTransactionSyncService _transactionSyncService;
    private readonly IPlaidAccessTokenProtector _accessTokenProtector;
    private readonly PlaidItemRemoval _itemRemoval;
    private readonly HouseholdScope _householdScope;
    private readonly ILogger<PlaidService> _logger;
    private readonly TimeProvider _timeProvider;

    public PlaidService(
        CarduiDBContext dbContext,
        IPlaidClientSource clientSource,
        IOptions<PlaidConfig> plaidOptions,
        IPlaidRequestExecutor requestExecutor,
        IPlaidAccountSyncService accountSyncService,
        IPlaidTransactionSyncService transactionSyncService,
        IPlaidAccessTokenProtector accessTokenProtector,
        PlaidItemRemoval itemRemoval,
        HouseholdScope householdScope,
        ILogger<PlaidService> logger,
        TimeProvider timeProvider,
        IPlaidLinkClient linkClient)
    {
        _dbContext = dbContext;
        _clientSource = clientSource;
        _plaidOptions = plaidOptions.Value;
        _requestExecutor = requestExecutor;
        _linkClient = linkClient;
        _accountSyncService = accountSyncService;
        _transactionSyncService = transactionSyncService;
        _accessTokenProtector = accessTokenProtector;
        _itemRemoval = itemRemoval;
        _householdScope = householdScope;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async Task<CreateLinkTokenResponseDto> CreateLinkTokenAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var request = NewLinkTokenRequest();
        request.Products = new List<Products>
        {
            Products.Transactions
        };
        var prepared = _requestExecutor.WithCredentials(request);

        return await LinkTokenAsync(prepared, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<CreateLinkTokenResponseDto> CreateUpdateLinkTokenAsync(
        Guid plaidItemId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var plaidItem = await GetPlaidItemOrThrowAsync(plaidItemId, cancellationToken);

        return await LinkTokenAsync(RepairLinkTokenRequest(plaidItem), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ExchangePublicTokenResponseDto> ExchangePublicTokenAsync(
        ExchangePublicTokenRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var client = _clientSource.GetClient();

        var request = _requestExecutor.WithCredentials(new ItemPublicTokenExchangeRequest
        {
            PublicToken = dto.PublicToken
        });

        var response = await _requestExecutor.ExecuteAsync(
            () => client.ItemPublicTokenExchangeAsync(request));

        var now = _timeProvider.GetUtcNow();

        var plaidItem = new PlaidItem
        {
            Id = Guid.NewGuid(),
            HouseholdId = _householdScope.RequireHouseholdId(),
            PlaidItemId = response.ItemId,
            AccessToken = _accessTokenProtector.Protect(response.AccessToken),
            InstitutionId = dto.InstitutionId,
            InstitutionName = dto.InstitutionName,
            CreatedAt = now,
            UpdatedAt = now
        };

        await using var dbTransaction = await _dbContext.Database
            .BeginTransactionAsync(cancellationToken);

        try
        {
            _dbContext.PlaidItems.Add(plaidItem);
            await RegisterWebhookAsync(plaidItem, cancellationToken);
            await _accountSyncService.SyncAccountsForPlaidItemAsync(
                plaidItem,
                cancellationToken);
            await _transactionSyncService.SyncTransactionsForPlaidItemAsync(
                plaidItem,
                cancellationToken);
            await dbTransaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await dbTransaction.RollbackAsync(cancellationToken);
            throw;
        }

        return new ExchangePublicTokenResponseDto
        {
            PlaidItemId = plaidItem.Id
        };
    }

    /// <inheritdoc />
    public async Task SyncAccountsAsync(
        Guid plaidItemId,
        CancellationToken cancellationToken = default)
    {
        var plaidItem = await GetPlaidItemOrThrowAsync(plaidItemId, cancellationToken);
        await _accountSyncService.SyncAccountsForPlaidItemAsync(
            plaidItem,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SyncTransactionsResponseDto> SyncTransactionsAsync(
        Guid plaidItemId,
        CancellationToken cancellationToken = default)
    {
        var plaidItem = await GetPlaidItemOrThrowAsync(plaidItemId, cancellationToken);
        return await _transactionSyncService.SyncTransactionsForPlaidItemAsync(
            plaidItem,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PlaidItemDto>> GetPlaidItemsAsync(
        CancellationToken cancellationToken = default)
    {
        var items = await _dbContext.PlaidItems
            .AsNoTracking()
            .InHousehold(_householdScope)
            .OrderBy(x => x.InstitutionName)
            .Select(x => new PlaidItemDto
            {
                Id = x.Id,
                InstitutionId = x.InstitutionId,
                InstitutionName = x.InstitutionName,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                LastTransactionsSyncedAt = x.LastTransactionsSyncedAt,
                LastSyncStartedAt = x.LastSyncStartedAt,
                LastSyncCompletedAt = x.LastSyncCompletedAt,
                LastSyncFailedAt = x.LastSyncFailedAt,
                LastSyncError = x.LastSyncError
            })
            .ToListAsync(cancellationToken);

        foreach (var item in items)
        {
            item.NeedsRepair = PlaidItemSync.LastAttemptFailed(
                item.LastSyncCompletedAt,
                item.LastSyncFailedAt);
        }

        return items;
    }

    /// <inheritdoc />
    public async Task<SyncPlaidItemResponseDto> SyncPlaidItemAsync(
        Guid plaidItemId,
        CancellationToken cancellationToken = default)
    {
        var plaidItem = await GetPlaidItemOrThrowAsync(plaidItemId, cancellationToken);
        if (!await TryClaimSyncAsync(plaidItem, cancellationToken))
        {
            return AlreadyRunning(plaidItem);
        }

        try
        {
            await _dbContext.Entry(plaidItem).ReloadAsync(cancellationToken);
            return await RunClaimedSyncAsync(plaidItem, cancellationToken);
        }
        catch (Exception ex)
        {
            await RecordSyncFailureAsync(plaidItem, ex);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task RemovePlaidItemAsync(
        Guid plaidItemId,
        CancellationToken cancellationToken = default)
    {
        var plaidItem = await GetPlaidItemOrThrowAsync(plaidItemId, cancellationToken);
        await RemoveAtPlaidAsync(plaidItem, cancellationToken);
        await _itemRemoval.RemoveStoredItemAsync(plaidItem, cancellationToken);
    }

    #region Private Methods

    /// <summary>
    /// The shared Link token fields for a new connection and for a repair.
    /// Products stay unset until the caller adds them. A repair leaves them unset.
    /// </summary>
    private LinkTokenCreateRequest NewLinkTokenRequest()
    {
        return new LinkTokenCreateRequest
        {
            ClientName = _plaidOptions.ClientName,
            Language = Language.English,
            CountryCodes = new List<CountryCode>
            {
                CountryCode.Us
            },
            User = new LinkTokenCreateRequestUser
            {
                ClientUserId = _householdScope.RequireHouseholdId().ToString("D")
            },
            Webhook = EmptyToNull(_plaidOptions.WebhookUrl)
        };
    }

    /// <summary>
    /// A Link token request for an existing item. Products stay unset, which
    /// is update mode. The access token is placed on the request for Plaid.
    /// </summary>
    private LinkTokenCreateRequest RepairLinkTokenRequest(PlaidItem plaidItem)
    {
        var accessToken = _accessTokenProtector.Unprotect(plaidItem.AccessToken);
        return _requestExecutor.WithCredentials(NewLinkTokenRequest(), accessToken);
    }

    /// <summary>
    /// Sends a prepared Link token request and returns the token.
    /// The access token on a repair request is not copied onto the response.
    /// </summary>
    private async Task<CreateLinkTokenResponseDto> LinkTokenAsync(
        LinkTokenCreateRequest request,
        CancellationToken cancellationToken)
    {
        var linkToken = await _linkClient.CreateAsync(request, cancellationToken);

        return new CreateLinkTokenResponseDto
        {
            LinkToken = linkToken
        };
    }

    /// <summary>
    /// Claims the item for this sync, or reports that another sync still holds it.
    /// An abandoned start is recorded as interrupted before the new claim.
    /// On PostgreSQL each step is a conditional update, so the worker and a
    /// manual sync cannot both claim the item.
    /// </summary>
    private async Task<bool> TryClaimSyncAsync(
        PlaidItem plaidItem,
        CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        var startedAt = now.AddTicks(1);
        if (!_dbContext.Database.IsRelational())
        {
            return await ClaimLoadedItemAsync(plaidItem, now, startedAt, cancellationToken);
        }

        var leaseCutoff = now - PlaidItemSync.Lease;
        await MarkInterruptedSyncAsync(plaidItem, now, leaseCutoff, cancellationToken);
        return await ClaimSyncAsync(plaidItem, startedAt, leaseCutoff, cancellationToken);
    }

    /// <summary>
    /// Applies <see cref="PlaidItemSync"/> to the loaded row and saves the claim.
    /// Used when the database cannot run a conditional update.
    /// </summary>
    private async Task<bool> ClaimLoadedItemAsync(
        PlaidItem plaidItem,
        DateTimeOffset now,
        DateTimeOffset startedAt,
        CancellationToken cancellationToken)
    {
        if (PlaidItemSync.HoldsItem(
                plaidItem.LastSyncStartedAt,
                plaidItem.LastSyncCompletedAt,
                plaidItem.LastSyncFailedAt,
                now))
        {
            return false;
        }

        if (PlaidItemSync.IsInterrupted(
                plaidItem.LastSyncStartedAt,
                plaidItem.LastSyncCompletedAt,
                plaidItem.LastSyncFailedAt,
                now))
        {
            plaidItem.LastSyncFailedAt = now;
            plaidItem.LastSyncError = PlaidItemSync.InterruptedMessage;
            plaidItem.UpdatedAt = now;
        }

        plaidItem.LastSyncStartedAt = startedAt;
        plaidItem.UpdatedAt = startedAt;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// Sets the start time when the item is free.
    /// A start that still holds the item is left unchanged.
    /// </summary>
    private async Task<bool> ClaimSyncAsync(
        PlaidItem plaidItem,
        DateTimeOffset startedAt,
        DateTimeOffset leaseCutoff,
        CancellationToken cancellationToken)
    {
        var claimed = await ClaimableItems(plaidItem, leaseCutoff)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(item => item.LastSyncStartedAt, startedAt)
                    .SetProperty(item => item.UpdatedAt, startedAt),
                cancellationToken);

        return claimed == 1;
    }

    /// <summary>
    /// Records an unfinished start whose lease has passed.
    /// The last success time stays, so freshness still has a completed sync.
    /// </summary>
    private async Task MarkInterruptedSyncAsync(
        PlaidItem plaidItem,
        DateTimeOffset now,
        DateTimeOffset leaseCutoff,
        CancellationToken cancellationToken)
    {
        await _dbContext.PlaidItems
            .Where(item => item.Id == plaidItem.Id && item.HouseholdId == plaidItem.HouseholdId)
            .Where(item =>
                item.LastSyncStartedAt != null
                && (item.LastSyncCompletedAt == null
                    || item.LastSyncStartedAt > item.LastSyncCompletedAt)
                && (item.LastSyncFailedAt == null
                    || item.LastSyncStartedAt > item.LastSyncFailedAt)
                && item.LastSyncStartedAt <= leaseCutoff)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(item => item.LastSyncFailedAt, now)
                    .SetProperty(item => item.LastSyncError, PlaidItemSync.InterruptedMessage)
                    .SetProperty(item => item.UpdatedAt, now),
                cancellationToken);
    }

    /// <summary>
    /// Items whose latest start is closed or older than the lease.
    /// Matches <see cref="PlaidItemSync.HoldsItem"/>: a holding start is excluded.
    /// </summary>
    private IQueryable<PlaidItem> ClaimableItems(PlaidItem plaidItem, DateTimeOffset leaseCutoff)
    {
        return _dbContext.PlaidItems
            .Where(item => item.Id == plaidItem.Id && item.HouseholdId == plaidItem.HouseholdId)
            .Where(item =>
                item.LastSyncStartedAt == null
                || (item.LastSyncCompletedAt != null
                    && item.LastSyncStartedAt <= item.LastSyncCompletedAt)
                || (item.LastSyncFailedAt != null
                    && item.LastSyncStartedAt <= item.LastSyncFailedAt)
                || item.LastSyncStartedAt <= leaseCutoff);
    }

    /// <summary>
    /// Syncs accounts and transactions after this call has claimed the item.
    /// Completion clears a previous failure. It does not erase an older success
    /// until the new sync finishes.
    /// </summary>
    private async Task<SyncPlaidItemResponseDto> RunClaimedSyncAsync(
        PlaidItem plaidItem,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Plaid sync for item {PlaidItemId}", plaidItem.Id);

        await RegisterWebhookAsync(plaidItem, cancellationToken);
        await _accountSyncService.SyncAccountsForPlaidItemAsync(
            plaidItem,
            cancellationToken);
        var transactionsResult = await _transactionSyncService.SyncTransactionsForPlaidItemAsync(
            plaidItem,
            cancellationToken);

        var completedAt = PlaidItemSync.FinishTime(
            plaidItem.LastSyncStartedAt,
            _timeProvider.GetUtcNow());
        plaidItem.LastSyncCompletedAt = completedAt;
        plaidItem.LastSyncFailedAt = null;
        plaidItem.LastSyncError = null;
        plaidItem.UpdatedAt = completedAt;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Completed Plaid sync for item {PlaidItemId}. Added {Added}, modified {Modified}, removed {Removed}",
            plaidItem.Id,
            transactionsResult.Added,
            transactionsResult.Modified,
            transactionsResult.Removed);

        return new SyncPlaidItemResponseDto
        {
            PlaidItemId = plaidItem.Id,
            Transactions = transactionsResult
        };
    }

    /// <summary>
    /// Returns without syncing. The in-progress sync keeps the accounts and timestamps.
    /// </summary>
    private static SyncPlaidItemResponseDto AlreadyRunning(PlaidItem plaidItem)
    {
        return new SyncPlaidItemResponseDto
        {
            PlaidItemId = plaidItem.Id,
            AlreadyRunning = true
        };
    }

    /// <summary>
    /// Loads a tracked household Plaid item, or throws when it is missing.
    /// </summary>
    private async Task<PlaidItem> GetPlaidItemOrThrowAsync(
        Guid plaidItemId,
        CancellationToken cancellationToken = default)
    {
        var plaidItem = await _dbContext.PlaidItems
            .InHousehold(_householdScope)
            .FirstOrDefaultAsync(x => x.Id == plaidItemId, cancellationToken);

        return plaidItem ?? throw new NotFoundException($"Plaid item '{plaidItemId}' was not found.");
    }

    /// <summary>
    /// Stores the failure time and a short error, and saves even when the
    /// request was cancelled. The last success time is left in place.
    /// </summary>
    private async Task RecordSyncFailureAsync(PlaidItem plaidItem, Exception ex)
    {
        RecordSyncFailure(plaidItem, ex);
        await _dbContext.SaveChangesAsync(CancellationToken.None);
    }

    /// <summary>
    /// Stores the failure time and a short error on the item. The caller saves.
    /// </summary>
    private void RecordSyncFailure(PlaidItem plaidItem, Exception ex)
    {
        _logger.LogError(
            "Plaid sync failed for item {PlaidItemId}. {ErrorType}",
            plaidItem.Id,
            ex.GetType().Name);

        var failedAt = PlaidItemSync.FinishTime(
            plaidItem.LastSyncStartedAt,
            _timeProvider.GetUtcNow());
        plaidItem.LastSyncFailedAt = failedAt;
        plaidItem.LastSyncError = FormatSyncError(ex);
        plaidItem.UpdatedAt = failedAt;
    }

    /// <summary>
    /// Returns the not-configured message, a Plaid error code for a
    /// PlaidSyncException, or a generic message otherwise.
    /// </summary>
    private static string FormatSyncError(Exception ex)
    {
        if (ex is OperationCanceledException)
        {
            return PlaidItemSync.InterruptedMessage;
        }

        if (ex is PlaidNotConfiguredException notConfigured)
        {
            return notConfigured.Message;
        }

        if (ex is not PlaidSyncException plaidSyncException) return "Sync failed. Please try again.";
        if (!string.IsNullOrWhiteSpace(plaidSyncException.PlaidErrorCode))
        {
            return string.IsNullOrWhiteSpace(plaidSyncException.PlaidErrorType)
                ? plaidSyncException.PlaidErrorCode
                : $"{plaidSyncException.PlaidErrorType}:{plaidSyncException.PlaidErrorCode}";
        }

        return plaidSyncException.Message;
    }

    /// <summary>
    /// Tells Plaid which webhook to call when a URL is configured.
    /// A registration failure is logged and does not stop the sync.
    /// </summary>
    private async Task RegisterWebhookAsync(
        PlaidItem plaidItem,
        CancellationToken cancellationToken)
    {
        var webhookUrl = EmptyToNull(_plaidOptions.WebhookUrl);
        if (webhookUrl is null)
        {
            return;
        }

        try
        {
            var client = _clientSource.GetClient();
            var accessToken = _accessTokenProtector.Unprotect(plaidItem.AccessToken);
            var request = _requestExecutor.WithCredentials(
                new ItemWebhookUpdateRequest
                {
                    Webhook = webhookUrl
                },
                accessToken);
            await _requestExecutor.ExecuteAsync(
                () => client.ItemWebhookUpdateAsync(request));
        }
        catch (PlaidSyncException exception)
        {
            _logger.LogWarning(
                "Plaid webhook registration failed for item {PlaidItemId}. {PlaidErrorType}/{PlaidErrorCode}",
                plaidItem.Id,
                exception.PlaidErrorType,
                exception.PlaidErrorCode);
        }
    }

    /// <summary>
    /// Revokes the access token at Plaid. An item Plaid has already removed
    /// is treated as success so the local token can still be deleted.
    /// </summary>
    private async Task RemoveAtPlaidAsync(
        PlaidItem plaidItem,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var client = _clientSource.GetClient();
        var accessToken = _accessTokenProtector.Unprotect(plaidItem.AccessToken);
        var request = _requestExecutor.WithCredentials(
            new ItemRemoveRequest(),
            accessToken);

        try
        {
            await _requestExecutor.ExecuteAsync(() => client.ItemRemoveAsync(request));
        }
        catch (PlaidSyncException exception) when (
            exception.PlaidErrorCode is "ITEM_NOT_FOUND" or "INVALID_ACCESS_TOKEN")
        {
            _logger.LogInformation(
                "Plaid item {PlaidItemId} was already removed at Plaid.",
                plaidItem.Id);
        }
    }

    /// <summary>
    /// Returns a trimmed URL, or null when the setting is blank.
    /// </summary>
    private static string? EmptyToNull(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    #endregion
}
