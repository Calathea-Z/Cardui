using Cardui.Api.Dtos.Plaid;

namespace Cardui.Api.Services.Interfaces;

public interface IPlaidService
{
    /// <summary>
    /// Asks Plaid for a Link token so the household can connect a bank.
    /// The Link user id is the household id.
    /// </summary>
    Task<CreateLinkTokenResponseDto> CreateLinkTokenAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exchanges a public token for an access token, stores the protected
    /// token, and syncs that item's accounts and transactions.
    /// The database work is rolled back if the first sync fails.
    /// </summary>
    Task<ExchangePublicTokenResponseDto> ExchangePublicTokenAsync(
        ExchangePublicTokenRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes accounts and today's balance snapshots for one household Plaid item.
    /// </summary>
    Task SyncAccountsAsync(Guid plaidItemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pulls transaction changes for one household Plaid item, categorizes
    /// new rows, and pairs transfers.
    /// </summary>
    Task<SyncTransactionsResponseDto> SyncTransactionsAsync(
        Guid plaidItemId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists the household's connected institutions and their latest sync status.
    /// Access tokens are not included.
    /// </summary>
    Task<IReadOnlyList<PlaidItemDto>> GetPlaidItemsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Syncs accounts and transactions for one item and records whether the
    /// sync completed or failed.
    /// </summary>
    Task<SyncPlaidItemResponseDto> SyncPlaidItemAsync(
        Guid plaidItemId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the bank login at Plaid and deletes the stored access token.
    /// Accounts and transactions stay in Cardui.
    /// </summary>
    Task RemovePlaidItemAsync(Guid plaidItemId, CancellationToken cancellationToken = default);
}
