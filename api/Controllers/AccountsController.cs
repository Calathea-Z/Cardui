using Cardui.Api.Dtos.Account;
using Cardui.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Cardui.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountsController : ControllerBase
{
    private readonly IAccountsService _accountsService;

    public AccountsController(IAccountsService accountsService)
    {
        _accountsService = accountsService;
    }

    /// <summary>
    /// GET /api/accounts
    /// Returns the household's accounts. Archived accounts are included
    /// only when includeArchived is true.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<AccountDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AccountDto>>> GetAccounts(
        [FromQuery] bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        var accounts = await _accountsService.GetAccountsAsync(
            includeArchived,
            cancellationToken);
        return Ok(accounts);
    }

    /// <summary>
    /// GET /api/accounts/summary
    /// Returns net worth, balances by type, balance history, and archived accounts.
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType<AccountSummaryDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AccountSummaryDto>> GetAccountsSummary(
        CancellationToken cancellationToken)
    {
        var summary = await _accountsService.GetAccountsSummaryAsync(cancellationToken);
        return Ok(summary);
    }

    /// <summary>
    /// POST /api/accounts
    /// Creates a manual account from a name, type, and opening balance.
    /// </summary>
    [HttpPost]
    [ProducesResponseType<AccountDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AccountDto>> CreateManualAccount(
        [FromBody] CreateManualAccountDto dto,
        CancellationToken cancellationToken)
    {
        var account = await _accountsService.CreateManualAccountAsync(dto, cancellationToken);
        return Ok(account);
    }

    /// <summary>
    /// PATCH /api/accounts/{id}
    /// Updates a manual account. Linked accounts cannot be edited this way.
    /// </summary>
    [HttpPatch("{id:guid}")]
    [ProducesResponseType<AccountDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AccountDto>> UpdateManualAccount(
        Guid id,
        [FromBody] UpdateManualAccountDto dto,
        CancellationToken cancellationToken)
    {
        var account = await _accountsService.UpdateManualAccountAsync(
            id,
            dto,
            cancellationToken);
        return Ok(account);
    }

    /// <summary>
    /// POST /api/accounts/{id}/archive
    /// Archives an account so its balance leaves totals and the chart.
    /// </summary>
    [HttpPost("{id:guid}/archive")]
    [ProducesResponseType<AccountDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AccountDto>> ArchiveAccount(
        Guid id,
        CancellationToken cancellationToken)
    {
        var account = await _accountsService.ArchiveAccountAsync(id, cancellationToken);
        return Ok(account);
    }

    /// <summary>
    /// POST /api/accounts/{id}/restore
    /// Restores an archived account so its balance counts again.
    /// </summary>
    [HttpPost("{id:guid}/restore")]
    [ProducesResponseType<AccountDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AccountDto>> RestoreAccount(
        Guid id,
        CancellationToken cancellationToken)
    {
        var account = await _accountsService.RestoreAccountAsync(id, cancellationToken);
        return Ok(account);
    }

    /// <summary>
    /// POST /api/accounts/{id}/reconciliation
    /// Matches a manual account to a statement balance and records any difference.
    /// </summary>
    [HttpPost("{id:guid}/reconciliation")]
    [ProducesResponseType<BalanceReconciliationResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BalanceReconciliationResultDto>> ReconcileBalance(
        Guid id,
        [FromBody] ReconcileAccountBalanceDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _accountsService.ReconcileBalanceAsync(
            id,
            dto,
            cancellationToken);
        return Ok(result);
    }
}
