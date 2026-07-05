using Cardui.Api.Dtos.Plaid;
using CardUI.Api.Services.Interfaces;
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
    public async Task<ActionResult<IReadOnlyList<PlaidItemDto>>> GetPlaidItems()
    {
        var items = await _plaidService.GetPlaidItemsAsync();
        return Ok(items);
    }

    [HttpPost("link-token")]
    public async Task<ActionResult<CreateLinkTokenResponseDto>> CreateLinkToken()
    {
        var result = await _plaidService.CreateLinkTokenAsync();
        return Ok(result);
    }

    [HttpPost("exchange-public-token")]
    public async Task<ActionResult<ExchangePublicTokenResponseDto>> ExchangePublicToken(
        ExchangePublicTokenRequestDto dto)
    {
        var result = await _plaidService.ExchangePublicTokenAsync(dto);
        return Ok(result);
    }

    [HttpPost("{plaidItemId:guid}/sync-accounts")]
    public async Task<IActionResult> SyncAccounts(Guid plaidItemId)
    {
        await _plaidService.SyncAccountsAsync(plaidItemId);
        return NoContent();
    }

    [HttpPost("{plaidItemId:guid}/sync-transactions")]
    public async Task<ActionResult<SyncTransactionsResponseDto>> SyncTransactions(
        Guid plaidItemId)
    {
        var result = await _plaidService.SyncTransactionsAsync(plaidItemId);
        return Ok(result);
    }

    [HttpPost("{plaidItemId:guid}/sync")]
    public async Task<ActionResult<SyncPlaidItemResponseDto>> SyncPlaidItem(
        Guid plaidItemId)
    {
        var result = await _plaidService.SyncPlaidItemAsync(plaidItemId);
        return Ok(result);
    }
}