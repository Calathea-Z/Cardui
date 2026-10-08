using Cardui.Api.Dtos.Savings;
using Cardui.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Cardui.Api.Controllers;

[ApiController]
[Route("api/savings-goals")]
public class SavingsGoalsController : ControllerBase
{
    private readonly ISavingsGoalsService _savingsGoalsService;

    public SavingsGoalsController(ISavingsGoalsService savingsGoalsService)
    {
        _savingsGoalsService = savingsGoalsService;
    }

    /// <summary>
    /// GET /api/savings-goals
    /// Returns the household's operating reserve, emergency goal, and sinking funds.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<SavingsGoalDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SavingsGoalDto>>> GetGoals(
        CancellationToken cancellationToken = default)
    {
        var goals = await _savingsGoalsService.GetGoalsAsync(cancellationToken);
        return Ok(goals);
    }

    /// <summary>
    /// GET /api/savings-goals/accounts
    /// Returns cash accounts a goal may follow.
    /// </summary>
    [HttpGet("accounts")]
    [ProducesResponseType<IReadOnlyList<SavingsAccountDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SavingsAccountDto>>> GetAccounts(
        CancellationToken cancellationToken = default)
    {
        var accounts = await _savingsGoalsService.GetAccountsAsync(cancellationToken);
        return Ok(accounts);
    }

    /// <summary>
    /// POST /api/savings-goals
    /// Records a goal. Saving it does not move money or create a transaction.
    /// </summary>
    [HttpPost]
    [ProducesResponseType<SavingsGoalDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SavingsGoalDto>> CreateGoal(
        [FromBody] UpsertSavingsGoalDto dto,
        CancellationToken cancellationToken)
    {
        var goal = await _savingsGoalsService.CreateAsync(dto, cancellationToken);
        return Ok(goal);
    }

    /// <summary>
    /// PUT /api/savings-goals/{id}
    /// Updates a goal's target, date, amount set aside, and followed account.
    /// The kind and the stored currency stay.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<SavingsGoalDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SavingsGoalDto>> UpdateGoal(
        Guid id,
        [FromBody] UpsertSavingsGoalDto dto,
        CancellationToken cancellationToken)
    {
        var goal = await _savingsGoalsService.UpdateAsync(id, dto, cancellationToken);
        return Ok(goal);
    }

    /// <summary>
    /// DELETE /api/savings-goals/{id}
    /// Deletes a goal. The account and its balance stay unchanged.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteGoal(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _savingsGoalsService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
