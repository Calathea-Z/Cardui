using Cardui.Api.Dtos.Plaid;

namespace Cardui.Api.Services.Interfaces;

public interface IPlaidService
{
    Task<CreateLinkTokenResponseDto> CreateLinkTokenAsync(
        CancellationToken cancellationToken = default);

    Task<ExchangePublicTokenResponseDto> ExchangePublicTokenAsync(
        ExchangePublicTokenRequestDto request,
        CancellationToken cancellationToken = default);

    Task SyncAccountsAsync(Guid plaidItemId, CancellationToken cancellationToken = default);

    Task<SyncTransactionsResponseDto> SyncTransactionsAsync(
        Guid plaidItemId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PlaidItemDto>> GetPlaidItemsAsync(
        CancellationToken cancellationToken = default);

    Task<SyncPlaidItemResponseDto> SyncPlaidItemAsync(
        Guid plaidItemId,
        CancellationToken cancellationToken = default);
}
