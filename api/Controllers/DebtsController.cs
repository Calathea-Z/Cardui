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
    /// A linked account balance is included when it differs. It is not copied onto the debt.
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
    /// POST /api/debts/{id}/use-account-balance
    /// Stores the linked account's dated balance on the debt.
    /// The account balance is not changed. APR, minimum, and due date stay as they are.
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
