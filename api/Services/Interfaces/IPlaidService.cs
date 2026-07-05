using Cardui.Api.Dtos.Plaid;

namespace Cardui.Api.Services.Interfaces;

public interface IPlaidService
{
    Task<CreateLinkTokenResponseDto> CreateLinkTokenAsync();
    Task<ExchangePublicTokenResponseDto> ExchangePublicTokenAsync(ExchangePublicTokenRequestDto request);
    Task SyncAccountsAsync(Guid plaidItemId);
    Task<SyncTransactionsResponseDto> SyncTransactionsAsync(Guid plaidItemId);
    Task<IReadOnlyList<PlaidItemDto>> GetPlaidItemsAsync();
    Task<SyncPlaidItemResponseDto> SyncPlaidItemAsync(Guid plaidItemId);
}