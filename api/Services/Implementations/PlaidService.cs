using Cardui.Api.Data;
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
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _clientSource = clientSource;
        _plaidOptions = plaidOptions.Value;
        _requestExecutor = requestExecutor;
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
        var client = _clientSource.GetClient();

        var request = _requestExecutor.WithCredentials(new LinkTokenCreateRequest
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
                ClientUserId = _householdScope.RequireHouseholdId().ToString("D")
            },
            Webhook = EmptyToNull(_plaidOptions.WebhookUrl)
        });

        var response = await _requestExecutor.ExecuteAsync(
            () => client.LinkTokenCreateAsync(request));

        return new CreateLinkTokenResponseDto
        {
            LinkToken = response.LinkToken
        };
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
        return await _dbContext.PlaidItems
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
    }

    /// <inheritdoc />
    public async Task<SyncPlaidItemResponseDto> SyncPlaidItemAsync(
        Guid plaidItemId,
        CancellationToken cancellationToken = default)
    {
        var plaidItem = await GetPlaidItemOrThrowAsync(plaidItemId, cancellationToken);
        await RegisterWebhookAsync(plaidItem, cancellationToken);
        var now = _timeProvider.GetUtcNow();

        plaidItem.LastSyncStartedAt = now;
        plaidItem.LastSyncCompletedAt = null;
        plaidItem.LastSyncFailedAt = null;
        plaidItem.LastSyncError = null;
        plaidItem.UpdatedAt = now;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Starting Plaid sync for item {PlaidItemId}", plaidItemId);

        try
        {
            await _accountSyncService.SyncAccountsForPlaidItemAsync(
                plaidItem,
                cancellationToken);
            var transactionsResult = await _transactionSyncService.SyncTransactionsForPlaidItemAsync(
                plaidItem,
                cancellationToken);

            var completedAt = _timeProvider.GetUtcNow();
            plaidItem.LastSyncCompletedAt = completedAt;
            plaidItem.LastSyncFailedAt = null;
            plaidItem.LastSyncError = null;
            plaidItem.UpdatedAt = completedAt;

            await _dbContext.SaveChangesAsync(cancellationToken);

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
            await _dbContext.SaveChangesAsync(cancellationToken);
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
    /// Stores the failure time and a short error on the item. The caller saves.
    /// </summary>
    private void RecordSyncFailure(PlaidItem plaidItem, Exception ex)
    {
        _logger.LogError(
            "Plaid sync failed for item {PlaidItemId}. {ErrorType}",
            plaidItem.Id,
            ex.GetType().Name);

        var failedAt = _timeProvider.GetUtcNow();
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
