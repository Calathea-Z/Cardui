using Cardui.Api.Dtos.Category;

namespace Cardui.Api.Services.Interfaces;

public interface ICategoriesService
{
    Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(
        CancellationToken cancellationToken = default);

    Task<CategoryDto> GetCategoryByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<CategoryDto> CreateCategoryAsync(
        CreateCategoryDto createCategoryDto,
        CancellationToken cancellationToken = default);

    Task<CategoryDto> UpdateCategoryAsync(
        Guid id,
        UpdateCategoryDto updateCategoryDto,
        CancellationToken cancellationToken = default);

    Task DeleteCategoryAsync(Guid id, CancellationToken cancellationToken = default);
}
