using Cardui.Api.Data;
using Cardui.Api.Dtos.Plaid;
using Cardui.Api.Exceptions;
using Cardui.Api.Models;
using Cardui.Api.Security;
using Cardui.Api.Services.Interfaces;
using Cardui.Api.Services.Plaid;
using Going.Plaid;
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
    private readonly PlaidClient _plaidClient;
    private readonly PlaidConfig _plaidOptions;
    private readonly IPlaidRequestExecutor _requestExecutor;
    private readonly IPlaidAccountSyncService _accountSyncService;
    private readonly IPlaidTransactionSyncService _transactionSyncService;
    private readonly IPlaidAccessTokenProtector _accessTokenProtector;
    private readonly HouseholdScope _householdScope;
    private readonly ILogger<PlaidService> _logger;
    private readonly TimeProvider _timeProvider;

    public PlaidService(
        CarduiDBContext dbContext,
        PlaidClient plaidClient,
        IOptions<PlaidConfig> plaidOptions,
        IPlaidRequestExecutor requestExecutor,
        IPlaidAccountSyncService accountSyncService,
        IPlaidTransactionSyncService transactionSyncService,
        IPlaidAccessTokenProtector accessTokenProtector,
        HouseholdScope householdScope,
        ILogger<PlaidService> logger,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _plaidClient = plaidClient;
        _plaidOptions = plaidOptions.Value;
        _requestExecutor = requestExecutor;
        _accountSyncService = accountSyncService;
        _transactionSyncService = transactionSyncService;
        _accessTokenProtector = accessTokenProtector;
        _householdScope = householdScope;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task<CreateLinkTokenResponseDto> CreateLinkTokenAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

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
            }
        });

        var response = await _requestExecutor.ExecuteAsync(
            () => _plaidClient.LinkTokenCreateAsync(request));

        return new CreateLinkTokenResponseDto
        {
            LinkToken = response.LinkToken
        };
    }

    public async Task<ExchangePublicTokenResponseDto> ExchangePublicTokenAsync(
        ExchangePublicTokenRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var request = _requestExecutor.WithCredentials(new ItemPublicTokenExchangeRequest
        {
            PublicToken = dto.PublicToken
        });

        var response = await _requestExecutor.ExecuteAsync(
            () => _plaidClient.ItemPublicTokenExchangeAsync(request));

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

    public async Task SyncAccountsAsync(
        Guid plaidItemId,
        CancellationToken cancellationToken = default)
    {
        var plaidItem = await GetPlaidItemOrThrowAsync(plaidItemId, cancellationToken);
        await _accountSyncService.SyncAccountsForPlaidItemAsync(
            plaidItem,
            cancellationToken);
    }

    public async Task<SyncTransactionsResponseDto> SyncTransactionsAsync(
        Guid plaidItemId,
        CancellationToken cancellationToken = default)
    {
        var plaidItem = await GetPlaidItemOrThrowAsync(plaidItemId, cancellationToken);
        return await _transactionSyncService.SyncTransactionsForPlaidItemAsync(
            plaidItem,
            cancellationToken);
    }

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

    public async Task<SyncPlaidItemResponseDto> SyncPlaidItemAsync(
        Guid plaidItemId,
        CancellationToken cancellationToken = default)
    {
        var plaidItem = await GetPlaidItemOrThrowAsync(plaidItemId, cancellationToken);
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

    private async Task<PlaidItem> GetPlaidItemOrThrowAsync(
        Guid plaidItemId,
        CancellationToken cancellationToken = default)
    {
        var plaidItem = await _dbContext.PlaidItems
            .InHousehold(_householdScope)
            .FirstOrDefaultAsync(x => x.Id == plaidItemId, cancellationToken);

        return plaidItem ?? throw new NotFoundException($"Plaid item '{plaidItemId}' was not found.");
    }

    private void RecordSyncFailure(PlaidItem plaidItem, Exception ex)
    {
        _logger.LogError(ex, "Plaid sync failed for item {PlaidItemId}", plaidItem.Id);

        var failedAt = _timeProvider.GetUtcNow();
        plaidItem.LastSyncFailedAt = failedAt;
        plaidItem.LastSyncError = FormatSyncError(ex);
        plaidItem.UpdatedAt = failedAt;
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
}
