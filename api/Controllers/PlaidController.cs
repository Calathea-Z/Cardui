using Cardui.Api.Dtos.Plaid;
using Cardui.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Cardui.Api.Controllers;

[ApiController]
[Route("api/plaid")]
public class PlaidController : ControllerBase
{
    private readonly IPlaidService _plaidService;

    public PlaidController(IPlaidService plaidService)
    {
        _plaidService = plaidService;
    }

    [HttpGet("items")]
    [ProducesResponseType<IReadOnlyList<PlaidItemDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPlaidItems()
    {
        var items = await _plaidService.GetPlaidItemsAsync();
        return Ok(items);
    }

    [HttpPost("link-token")]
    [ProducesResponseType<CreateLinkTokenResponseDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateLinkToken()
    {
        var result = await _plaidService.CreateLinkTokenAsync();
        return Ok(result);
    }

    [HttpPost("exchange-public-token")]
    [ProducesResponseType<ExchangePublicTokenResponseDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExchangePublicToken(
        ExchangePublicTokenRequestDto dto)
    {
        var result = await _plaidService.ExchangePublicTokenAsync(dto);
        return Ok(result);
    }

    [HttpPost("{plaidItemId:guid}/sync-accounts")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SyncAccounts(Guid plaidItemId)
    {
        await _plaidService.SyncAccountsAsync(plaidItemId);
        return NoContent();
    }

    [HttpPost("{plaidItemId:guid}/sync-transactions")]
    [ProducesResponseType<SyncTransactionsResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SyncTransactions(Guid plaidItemId)
    {
        var result = await _plaidService.SyncTransactionsAsync(plaidItemId);
        return Ok(result);
    }

    [HttpPost("{plaidItemId:guid}/sync")]
    [ProducesResponseType<SyncPlaidItemResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SyncPlaidItem(Guid plaidItemId)
    {
        var result = await _plaidService.SyncPlaidItemAsync(plaidItemId);
        return Ok(result);
    }
}
