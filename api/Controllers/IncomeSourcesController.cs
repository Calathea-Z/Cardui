using Cardui.Api.Dtos.Income;
using Cardui.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Cardui.Api.Controllers;

[ApiController]
[Route("api/income-sources")]
public class IncomeSourcesController : ControllerBase
{
    private readonly IIncomeSourcesService _incomeSourcesService;

    public IncomeSourcesController(IIncomeSourcesService incomeSourcesService)
    {
        _incomeSourcesService = incomeSourcesService;
    }

    /// <summary>
    /// GET /api/income-sources
    /// Returns the household's income sources.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<IncomeSourceDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<IncomeSourceDto>>> GetIncomeSources(
        CancellationToken cancellationToken = default)
    {
        var sources = await _incomeSourcesService.GetIncomeSourcesAsync(cancellationToken);
        return Ok(sources);
    }

    /// <summary>
    /// POST /api/income-sources
    /// Records an income source: typical take-home for one payment, optional low
    /// and strong amounts, cadence, next date, contributor, reliability, and
    /// expected raises.
    /// </summary>
    [HttpPost]
    [ProducesResponseType<IncomeSourceDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IncomeSourceDto>> CreateIncomeSource(
        [FromBody] UpsertIncomeSourceDto dto,
        CancellationToken cancellationToken)
    {
        var source = await _incomeSourcesService.CreateAsync(dto, cancellationToken);
        return Ok(source);
    }

    /// <summary>
    /// PUT /api/income-sources/{id}
    /// Updates an income source's payment facts, scenarios, and expected raises.
    /// The stored currency stays. Raises omitted from the request are removed.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<IncomeSourceDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IncomeSourceDto>> UpdateIncomeSource(
        Guid id,
        [FromBody] UpsertIncomeSourceDto dto,
        CancellationToken cancellationToken)
    {
        var source = await _incomeSourcesService.UpdateAsync(id, dto, cancellationToken);
        return Ok(source);
    }

    /// <summary>
    /// DELETE /api/income-sources/{id}
    /// Deletes an income source and its expected raises. Balances stay unchanged.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteIncomeSource(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _incomeSourcesService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
