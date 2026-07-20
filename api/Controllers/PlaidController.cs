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
    public async Task<ActionResult<IReadOnlyList<PlaidItemDto>>> GetPlaidItems(
        CancellationToken cancellationToken)
    {
        var items = await _plaidService.GetPlaidItemsAsync(cancellationToken);
        return Ok(items);
    }

    [HttpPost("link-token")]
    [ProducesResponseType<CreateLinkTokenResponseDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CreateLinkTokenResponseDto>> CreateLinkToken(
        CancellationToken cancellationToken)
    {
        var result = await _plaidService.CreateLinkTokenAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost("exchange-public-token")]
    [ProducesResponseType<ExchangePublicTokenResponseDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ExchangePublicTokenResponseDto>> ExchangePublicToken(
        ExchangePublicTokenRequestDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _plaidService.ExchangePublicTokenAsync(dto, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{plaidItemId:guid}/sync-accounts")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SyncAccounts(
        Guid plaidItemId,
        CancellationToken cancellationToken)
    {
        await _plaidService.SyncAccountsAsync(plaidItemId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{plaidItemId:guid}/sync-transactions")]
    [ProducesResponseType<SyncTransactionsResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SyncTransactionsResponseDto>> SyncTransactions(
        Guid plaidItemId,
        CancellationToken cancellationToken)
    {
        var result = await _plaidService.SyncTransactionsAsync(
            plaidItemId,
            cancellationToken);
        return Ok(result);
    }

    [HttpPost("{plaidItemId:guid}/sync")]
    [ProducesResponseType<SyncPlaidItemResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SyncPlaidItemResponseDto>> SyncPlaidItem(
        Guid plaidItemId,
        CancellationToken cancellationToken)
    {
        var result = await _plaidService.SyncPlaidItemAsync(
            plaidItemId,
            cancellationToken);
        return Ok(result);
    }
}
