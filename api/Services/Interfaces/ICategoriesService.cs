using Cardui.Api.Dtos.Category;

namespace Cardui.Api.Services.Interfaces;

public interface ICategoriesService
{
    Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync();
}