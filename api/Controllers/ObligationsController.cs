using Cardui.Api.Dtos.Obligations;
using Cardui.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Cardui.Api.Controllers;

[ApiController]
[Route("api/obligations")]
public class ObligationsController : ControllerBase
{
    private readonly IObligationsService _obligationsService;

    public ObligationsController(IObligationsService obligationsService)
    {
        _obligationsService = obligationsService;
    }

    /// <summary>
    /// GET /api/obligations
    /// Returns the household's bills. Each amount is one payment.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ObligationDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ObligationDto>>> GetObligations(
        CancellationToken cancellationToken = default)
    {
        var obligations = await _obligationsService.GetObligationsAsync(cancellationToken);
        return Ok(obligations);
    }

    /// <summary>
    /// POST /api/obligations
    /// Records a bill: one payment, cadence, next due date, optional source account,
    /// and whether it is essential or flexible.
    /// </summary>
    [HttpPost]
    [ProducesResponseType<ObligationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ObligationDto>> CreateObligation(
        [FromBody] UpsertObligationDto dto,
        CancellationToken cancellationToken)
    {
        var obligation = await _obligationsService.CreateAsync(dto, cancellationToken);
        return Ok(obligation);
    }

    /// <summary>
    /// PUT /api/obligations/{id}
    /// Updates a bill's payment facts. The stored currency stays.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<ObligationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ObligationDto>> UpdateObligation(
        Guid id,
        [FromBody] UpsertObligationDto dto,
        CancellationToken cancellationToken)
    {
        var obligation = await _obligationsService.UpdateAsync(id, dto, cancellationToken);
        return Ok(obligation);
    }

    /// <summary>
    /// DELETE /api/obligations/{id}
    /// Deletes a bill. The source account and its balance stay unchanged.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteObligation(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _obligationsService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
