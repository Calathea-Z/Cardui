using Cardui.Api.Domain;
using Cardui.Api.Dtos.Debts;
using Cardui.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Cardui.Api.Controllers;

[ApiController]
[Route("api/debts")]
public class DebtsController : ControllerBase
{
    private readonly IDebtsService _debtsService;

    public DebtsController(IDebtsService debtsService)
    {
        _debtsService = debtsService;
    }

    /// <summary>
    /// GET /api/debts
    /// Returns the household's debts. Unknown terms stay null.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<DebtDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DebtDto>>> GetDebts(
        CancellationToken cancellationToken = default)
    {
        var debts = await _debtsService.GetDebtsAsync(cancellationToken);
        return Ok(debts);
    }

    /// <summary>
    /// POST /api/debts
    /// Records a debt: type, optional linked account, dated balance, and optional terms.
    /// A blank term is stored as unknown. The linked account balance is not changed.
    /// </summary>
    [HttpPost]
    [ProducesResponseType<DebtDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DebtDto>> CreateDebt(
        [FromBody] UpsertDebtDto dto,
        CancellationToken cancellationToken)
    {
        var debt = await _debtsService.CreateAsync(dto, cancellationToken);
        return Ok(debt);
    }

    /// <summary>
    /// PUT /api/debts/{id}
    /// Updates a debt's facts. The stored currency stays.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<DebtDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DebtDto>> UpdateDebt(
        Guid id,
        [FromBody] UpsertDebtDto dto,
        CancellationToken cancellationToken)
    {
        var debt = await _debtsService.UpdateAsync(id, dto, cancellationToken);
        return Ok(debt);
    }

    /// <summary>
    /// GET /api/debts/summary
    /// Returns totals, risks, and missing inputs for the household's debts.
    /// A reference link shows the account balance when it differs. A followed balance is the one in use.
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType<DebtSummaryReportDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<DebtSummaryReportDto>> GetDebtSummary(
        CancellationToken cancellationToken = default)
    {
        var summary = await _debtsService.GetSummaryAsync(cancellationToken);
        return Ok(summary);
    }

    /// <summary>
    /// GET /api/debts/{id}/follow-accounts
    /// Lists the connected credit cards and loans this debt may follow.
    /// </summary>
    [HttpGet("{id:guid}/follow-accounts")]
    [ProducesResponseType<IReadOnlyList<DebtFollowAccountDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<DebtFollowAccountDto>>> GetFollowAccounts(
        Guid id,
        CancellationToken cancellationToken)
    {
        var accounts = await _debtsService.GetFollowAccountsAsync(id, cancellationToken);
        return Ok(accounts);
    }

    /// <summary>
    /// POST /api/debts/{id}/follow
    /// Makes the debt follow one connected account. The stored balance is not replaced by sync.
    /// </summary>
    [HttpPost("{id:guid}/follow")]
    [ProducesResponseType<DebtDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DebtDto>> FollowAccount(
        Guid id,
        [FromBody] FollowDebtAccountDto dto,
        CancellationToken cancellationToken)
    {
        var debt = await _debtsService.FollowAccountAsync(id, dto, cancellationToken);
        return Ok(debt);
    }

    /// <summary>
    /// PUT /api/debts/{id}/overrides/{field}
    /// Keeps the person's balance while the debt follows an account.
    /// A missing date means today. The debt's other terms are not changed.
    /// </summary>
    [HttpPut("{id:guid}/overrides/{field}")]
    [ProducesResponseType<DebtDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DebtDto>> SetOverride(
        Guid id,
        DebtSyncedField field,
        [FromBody] SetDebtBalanceOverrideDto dto,
        CancellationToken cancellationToken)
    {
        var debt = await _debtsService.SetOverrideAsync(id, field, dto, cancellationToken);
        return Ok(debt);
    }

    /// <summary>
    /// DELETE /api/debts/{id}/overrides/{field}
    /// Clears the person's balance so the debt uses the synced balance again.
    /// </summary>
    [HttpDelete("{id:guid}/overrides/{field}")]
    [ProducesResponseType<DebtDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DebtDto>> ClearOverride(
        Guid id,
        DebtSyncedField field,
        CancellationToken cancellationToken)
    {
        var debt = await _debtsService.ClearOverrideAsync(id, field, cancellationToken);
        return Ok(debt);
    }

    /// <summary>
    /// POST /api/debts/{id}/stop-following
    /// Stops following and keeps the last balance on the debt. The account stays linked.
    /// </summary>
    [HttpPost("{id:guid}/stop-following")]
    [ProducesResponseType<DebtDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DebtDto>> StopFollowing(
        Guid id,
        CancellationToken cancellationToken)
    {
        var debt = await _debtsService.StopFollowingAsync(id, cancellationToken);
        return Ok(debt);
    }

    /// <summary>
    /// POST /api/debts/{id}/use-account-balance
    /// Uses the linked account's balance. An eligible account starts a follow.
    /// An account that cannot be followed is copied onto the debt once. The account itself is not changed.
    /// </summary>
    [HttpPost("{id:guid}/use-account-balance")]
    [ProducesResponseType<DebtDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DebtDto>> UseAccountBalance(
        Guid id,
        CancellationToken cancellationToken)
    {
        var debt = await _debtsService.UseAccountBalanceAsync(id, cancellationToken);
        return Ok(debt);
    }

    /// <summary>
    /// DELETE /api/debts/{id}
    /// Deletes a debt. The linked account and its balance stay unchanged.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteDebt(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _debtsService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
