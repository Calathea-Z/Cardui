using Cardui.Api.Dtos.Category;

namespace Cardui.Api.Services.Interfaces;

public interface ICategoriesService
{
    /// <summary>
    /// Lists system categories and categories owned by the signed-in household.
    /// </summary>
    Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns one category the household is allowed to see.
    /// </summary>
    Task<CategoryDto> GetCategoryByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a household category under a visible sub-group.
    /// The name and derived key must be unique among system categories and this household.
    /// </summary>
    Task<CategoryDto> CreateCategoryAsync(
        CreateCategoryDto createCategoryDto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Renames a household category and updates its sub-group, color, and icon.
    /// System categories cannot be changed. The stored key is left unchanged.
    /// </summary>
    Task<CategoryDto> UpdateCategoryAsync(
        Guid id,
        UpdateCategoryDto updateCategoryDto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a household category and clears it from the household's
    /// transactions. System categories cannot be deleted.
    /// </summary>
    Task DeleteCategoryAsync(Guid id, CancellationToken cancellationToken = default);
}
