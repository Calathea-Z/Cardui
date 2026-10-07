using Cardui.Api.Dtos.CategoryTargets;
using Cardui.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Cardui.Api.Controllers;

[ApiController]
[Route("api/category-targets")]
public class CategoryTargetsController : ControllerBase
{
    private readonly ICategoryTargetsService _categoryTargetsService;

    public CategoryTargetsController(ICategoryTargetsService categoryTargetsService)
    {
        _categoryTargetsService = categoryTargetsService;
    }

    /// <summary>
    /// GET /api/category-targets
    /// Returns targets, spent, and remaining for one month.
    /// Omitting the year and month uses the household's current month.
    /// A month that has not been started is a preview and is not saved.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<CategoryTargetMonthDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CategoryTargetMonthDto>> GetMonth(
        [FromQuery] int? year,
        [FromQuery] int? month,
        CancellationToken cancellationToken)
    {
        var result = await _categoryTargetsService.GetAsync(year, month, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// POST /api/category-targets/copy-forward
    /// Starts the month with the nearest earlier month's targets and rollover choices.
    /// Leftover money is not copied. A month that already started is unchanged.
    /// </summary>
    [HttpPost("copy-forward")]
    [ProducesResponseType<CategoryTargetMonthDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CategoryTargetMonthDto>> CopyForward(
        [FromBody] CategoryTargetMonthRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _categoryTargetsService.CopyForwardAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// POST /api/category-targets/start-fresh
    /// Starts the month with no targets so a later visit does not copy an earlier month.
    /// </summary>
    [HttpPost("start-fresh")]
    [ProducesResponseType<CategoryTargetMonthDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CategoryTargetMonthDto>> StartFresh(
        [FromBody] CategoryTargetMonthRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _categoryTargetsService.StartFreshAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// PUT /api/category-targets/{categoryId}
    /// Saves one category's target and rollover choice.
    /// The first save in a month also copies the other categories from the nearest earlier month.
    /// </summary>
    [HttpPut("{categoryId:guid}")]
    [ProducesResponseType<CategoryTargetMonthDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryTargetMonthDto>> Save(
        Guid categoryId,
        [FromBody] UpsertCategoryTargetDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _categoryTargetsService.SaveAsync(categoryId, dto, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// DELETE /api/category-targets/{categoryId}
    /// Removes one category's target. The month stays started.
    /// </summary>
    [HttpDelete("{categoryId:guid}")]
    [ProducesResponseType<CategoryTargetMonthDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryTargetMonthDto>> Clear(
        Guid categoryId,
        [FromQuery] int year,
        [FromQuery] int month,
        CancellationToken cancellationToken)
    {
        var result = await _categoryTargetsService.ClearAsync(
            categoryId,
            year,
            month,
            cancellationToken);
        return Ok(result);
    }
}
