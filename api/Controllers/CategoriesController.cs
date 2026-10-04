using Cardui.Api.Dtos.Category;
using Cardui.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Cardui.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoriesService _categoriesService;

    public CategoriesController(ICategoriesService categoriesService)
    {
        _categoriesService = categoriesService;
    }

    /// <summary>
    /// GET /api/categories
    /// Lists categories the household can use.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CategoryDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> GetCategories(
        CancellationToken cancellationToken)
    {
        var categories = await _categoriesService.GetCategoriesAsync(cancellationToken);
        return Ok(categories);
    }

    /// <summary>
    /// GET /api/categories/{id}
    /// Returns one visible category.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<CategoryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryDto>> GetCategoryById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var category = await _categoriesService.GetCategoryByIdAsync(
            id,
            cancellationToken);
        return Ok(category);
    }

    /// <summary>
    /// POST /api/categories
    /// Creates a household category.
    /// </summary>
    [HttpPost]
    [ProducesResponseType<CategoryDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CategoryDto>> CreateCategory(
        [FromBody] CreateCategoryDto dto,
        CancellationToken cancellationToken)
    {
        var category = await _categoriesService.CreateCategoryAsync(
            dto,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetCategoryById),
            new { id = category.Id },
            category);
    }

    /// <summary>
    /// PATCH /api/categories/{id}
    /// Updates a category's name, sub-group, color, and icon.
    /// </summary>
    [HttpPatch("{id:guid}")]
    [ProducesResponseType<CategoryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryDto>> UpdateCategory(
        Guid id,
        [FromBody] UpdateCategoryDto dto,
        CancellationToken cancellationToken)
    {
        var category = await _categoriesService.UpdateCategoryAsync(
            id,
            dto,
            cancellationToken);
        return Ok(category);
    }

    /// <summary>
    /// DELETE /api/categories/{id}
    /// Deletes a household category and clears it from transactions.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCategory(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _categoriesService.DeleteCategoryAsync(id, cancellationToken);
        return NoContent();
    }
}
