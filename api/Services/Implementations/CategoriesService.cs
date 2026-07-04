using Cardui.Api.Data;
using Cardui.Api.Dtos.Category;
using Cardui.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cardui.Api.Services.Implementations;


public class CategoriesService : ICategoriesService
{
    private readonly CarduiDBContext _dbContext;

    public CategoriesService(CarduiDBContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync()
    {
        return await _dbContext.Categories
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new CategoryDto
            {
                Id = x.Id,
                Name = x.Name,
                ParentCategoryId = x.ParentCategoryId,
                Color = x.Color,
                Icon = x.Icon,
                IsSystem = x.IsSystem
            })
            .ToListAsync();
    }
}