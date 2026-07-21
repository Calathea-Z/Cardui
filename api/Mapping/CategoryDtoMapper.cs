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
        Key = x.Key,
        SubGroupId = x.SubGroupId,
        Color = x.Color,
        Icon = x.Icon,
        IsSystem = x.IsSystem
    };

    public static CategoryDto MapToDto(Category category)
    {
        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Key = category.Key,
            SubGroupId = category.SubGroupId,
            Color = category.Color,
            Icon = category.Icon,
            IsSystem = category.IsSystem
        };
    }
}
