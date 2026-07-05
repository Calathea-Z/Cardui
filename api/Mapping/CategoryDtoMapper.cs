using System.Linq.Expressions;
using Cardui.Api.Dtos.Category;
using Cardui.Api.Models;

namespace Cardui.Api.Mapping;

public static class CategoryDtoMapper
{
    public static readonly Expression<Func<Category, CategoryDto>> Projection = x => new CategoryDto
    {
        Id = x.Id,
        Name = x.Name,
        ParentCategoryId = x.ParentCategoryId,
        Color = x.Color,
        Icon = x.Icon,
        IsSystem = x.IsSystem
    };

    public static CategoryDto MapToDto(Category category) => new()
    {
        Id = category.Id,
        Name = category.Name,
        ParentCategoryId = category.ParentCategoryId,
        Color = category.Color,
        Icon = category.Icon,
        IsSystem = category.IsSystem
    };
}
