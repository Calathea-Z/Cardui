using Cardui.Api.Configuration;
using Cardui.Api.Dtos.Plaid;
using Cardui.Api.Services.Interfaces;
using Cardui.Api.Services.Plaid;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Cardui.Api.Controllers;

[ApiController]
[EnableRateLimiting(CarduiRateLimiting.PlaidPolicy)]
[Route("api/plaid")]
public class PlaidController : ControllerBase
{
    private readonly IPlaidService _plaidService;
    private readonly IPlaidWebhookService _webhookService;

    public PlaidController(IPlaidService plaidService, IPlaidWebhookService webhookService)
    {
        _plaidService = plaidService;
        _webhookService = webhookService;
    }

    /// <summary>
    /// GET /api/plaid/items
    /// Lists the household's connected institutions and their sync status.
    /// </summary>
    [HttpGet("items")]
    [ProducesResponseType<IReadOnlyList<PlaidItemDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PlaidItemDto>>> GetPlaidItems(
        CancellationToken cancellationToken)
    {
        var items = await _plaidService.GetPlaidItemsAsync(cancellationToken);
        return Ok(items);
    }

    /// <summary>
    /// POST /api/plaid/link-token
    /// Creates a Plaid Link token for connecting a bank.
    /// </summary>
    [HttpPost("link-token")]
    [ProducesResponseType<CreateLinkTokenResponseDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CreateLinkTokenResponseDto>> CreateLinkToken(
        CancellationToken cancellationToken)
    {
        var result = await _plaidService.CreateLinkTokenAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// POST /api/plaid/{plaidItemId}/link-token
    /// Creates a Plaid Link token that repairs one existing bank connection.
    /// The access token is not returned.
    /// </summary>
    [HttpPost("{plaidItemId:guid}/link-token")]
    [ProducesResponseType<CreateLinkTokenResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CreateLinkTokenResponseDto>> CreateUpdateLinkToken(
        Guid plaidItemId,
        CancellationToken cancellationToken)
    {
        var result = await _plaidService.CreateUpdateLinkTokenAsync(
            plaidItemId,
            cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// POST /api/plaid/exchange-public-token
    /// Saves a new bank connection and runs the first account and transaction sync.
    /// </summary>
    [HttpPost("exchange-public-token")]
    [ProducesResponseType<ExchangePublicTokenResponseDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ExchangePublicTokenResponseDto>> ExchangePublicToken(
        ExchangePublicTokenRequestDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _plaidService.ExchangePublicTokenAsync(dto, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// POST /api/plaid/{plaidItemId}/sync-accounts
    /// Refreshes accounts for one connected institution.
    /// </summary>
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

    /// <summary>
    /// POST /api/plaid/{plaidItemId}/sync-transactions
    /// Pulls transaction changes for one connected institution.
    /// </summary>
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

    /// <summary>
    /// POST /api/plaid/{plaidItemId}/sync
    /// Syncs accounts and transactions for one institution and records the result.
    /// </summary>
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

    /// <summary>
    /// DELETE /api/plaid/{plaidItemId}
    /// Removes the bank login at Plaid and deletes the stored access token.
    /// Accounts and transactions stay in Cardui.
    /// </summary>
    [HttpDelete("{plaidItemId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemovePlaidItem(
        Guid plaidItemId,
        CancellationToken cancellationToken)
    {
        await _plaidService.RemovePlaidItemAsync(plaidItemId, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// POST /api/plaid/webhook
    /// Accepts a signed Plaid webhook for revoked consent or a connection warning.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("webhook")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ReceiveWebhook(CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        await Request.Body.CopyToAsync(buffer, cancellationToken);
        var jwt = Request.Headers["Plaid-Verification"].ToString();
        await _webhookService.AcceptAsync(jwt, buffer.ToArray(), cancellationToken);
        return Ok();
    }
}
