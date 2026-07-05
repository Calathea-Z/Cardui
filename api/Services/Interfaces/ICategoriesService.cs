using Cardui.Api.Dtos.Category;

namespace Cardui.Api.Services.Interfaces;

public interface ICategoriesService
{
    Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync();
    Task<CategoryDto> GetCategoryByIdAsync(Guid id);
    Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto createCategoryDto);
    Task<CategoryDto> UpdateCategoryAsync(Guid id, UpdateCategoryDto updateCategoryDto);
    Task DeleteCategoryAsync(Guid id);
}
