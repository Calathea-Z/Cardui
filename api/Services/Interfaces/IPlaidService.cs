using Cardui.Api.Dtos.Plaid;

namespace CardUI.Api.Services.Interfaces;

public interface IPlaidService
{
    Task<CreateLinkTokenResponseDto> CreateLinkTokenAsync();
    Task<ExchangePublicTokenResponseDto> ExchangePublicTokenAsync(ExchangePublicTokenRequestDto request);
}