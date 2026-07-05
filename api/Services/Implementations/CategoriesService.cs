using Cardui.Api.Data;
using Cardui.Api.Dtos.Category;
using Cardui.Api.Exceptions;
using Cardui.Api.Mapping;
using Cardui.Api.Models;
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
            .Select(CategoryDtoMapper.Projection)
            .ToListAsync();
    }

    public Task<CategoryDto> GetCategoryByIdAsync(Guid id)
    {
        return ProjectCategoryByIdAsync(id);
    }

    public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto createCategoryDto)
    {
        var name = createCategoryDto.Name.Trim();

        if (string.IsNullOrWhiteSpace(name)) throw new BadRequestException("Category name is required.");

        var nameExists = await _dbContext.Categories
            .AnyAsync(x => x.Name.ToLower() == name.ToLower());

        if (nameExists) throw new BadRequestException("A category with this name already exists.");

        await ValidateParentCategoryAsync(createCategoryDto.ParentCategoryId);

        var key = CreateCategoryKey(name);

        var keyExists = await _dbContext.Categories
            .AnyAsync(x => x.Key == key);

        if (keyExists) throw new BadRequestException("A category with this key already exists.");

        var now = DateTimeOffset.UtcNow;

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = name,
            Key = key,
            ParentCategoryId = createCategoryDto.ParentCategoryId,
            Color = createCategoryDto.Color,
            Icon = createCategoryDto.Icon,
            IsSystem = false,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.Categories.Add(category);
        await _dbContext.SaveChangesAsync();

        return CategoryDtoMapper.MapToDto(category);
    }

    public async Task<CategoryDto> UpdateCategoryAsync(Guid id, UpdateCategoryDto dto)
    {
        var category = await _dbContext.Categories
            .FirstOrDefaultAsync(x => x.Id == id);

        if (category is null) throw new NotFoundException($"Category '{id}' was not found.");

        var name = dto.Name.Trim();

        if (string.IsNullOrWhiteSpace(name)) throw new BadRequestException("Category name is required.");

        var nameExists = await _dbContext.Categories
            .AnyAsync(x => x.Id != id && x.Name.ToLower() == name.ToLower());

        if (nameExists) throw new BadRequestException("A category with this name already exists.");

        if (dto.ParentCategoryId == id) throw new BadRequestException("A category cannot be its own parent.");

        await ValidateParentCategoryAsync(dto.ParentCategoryId);

        category.Name = name;
        category.ParentCategoryId = dto.ParentCategoryId;
        category.Color = dto.Color;
        category.Icon = dto.Icon;
        category.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync();

        return CategoryDtoMapper.MapToDto(category);
    }

    public async Task DeleteCategoryAsync(Guid id)
    {
        var category = await _dbContext.Categories
            .FirstOrDefaultAsync(x => x.Id == id);

        if (category is null) throw new NotFoundException($"Category '{id}' was not found.");

        if (category.IsSystem) throw new BadRequestException("System categories cannot be deleted.");

        var now = DateTimeOffset.UtcNow;

        var transactions = await _dbContext.Transactions
            .Where(x => x.CategoryId == id)
            .ToListAsync();

        foreach (var transaction in transactions)
        {
            transaction.CategoryId = null;
            transaction.UpdatedAt = now;
        }

        var childCategories = await _dbContext.Categories
            .Where(x => x.ParentCategoryId == id)
            .ToListAsync();

        foreach (var childCategory in childCategories)
        {
            childCategory.ParentCategoryId = null;
            childCategory.UpdatedAt = now;
        }

        _dbContext.Categories.Remove(category);

        await _dbContext.SaveChangesAsync();
    }

    #region Private Methods

    private async Task ValidateParentCategoryAsync(Guid? parentCategoryId)
    {
        if (!parentCategoryId.HasValue) return;

        var parentExists = await _dbContext.Categories
            .AnyAsync(x => x.Id == parentCategoryId.Value);

        if (!parentExists) throw new BadRequestException($"Parent category '{parentCategoryId}' was not found.");
    }

    private async Task<CategoryDto> ProjectCategoryByIdAsync(Guid id)
    {
        var category = await _dbContext.Categories
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(CategoryDtoMapper.Projection)
            .FirstOrDefaultAsync();

        if (category is null) throw new NotFoundException($"Category '{id}' was not found.");

        return category;
    }

    private static string CreateCategoryKey(string name)
    {
        return name
            .Trim()
            .ToLowerInvariant()
            .Replace(" ", "-");
    }

    #endregion
}