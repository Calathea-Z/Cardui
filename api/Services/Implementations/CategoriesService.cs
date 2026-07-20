using Cardui.Api.Data;
using Cardui.Api.Domain;
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
    private readonly TimeProvider _timeProvider;

    public CategoriesService(CarduiDBContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Categories
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(CategoryDtoMapper.Projection)
            .ToListAsync(cancellationToken);
    }

    public Task<CategoryDto> GetCategoryByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return ProjectCategoryByIdAsync(id, cancellationToken);
    }

    public async Task<CategoryDto> CreateCategoryAsync(
        CreateCategoryDto createCategoryDto,
        CancellationToken cancellationToken = default)
    {
        var name = createCategoryDto.Name.Trim();

        if (string.IsNullOrWhiteSpace(name)) throw new BadRequestException("Category name is required.");

        var nameExists = await _dbContext.Categories
            .AnyAsync(x => EF.Functions.ILike(x.Name, name), cancellationToken);

        if (nameExists) throw new BadRequestException("A category with this name already exists.");

        await ValidateParentCategoryAsync(
            createCategoryDto.ParentCategoryId,
            cancellationToken);

        var key = CategoryKeys.CreateFromName(name);

        var keyExists = await _dbContext.Categories
            .AnyAsync(x => x.Key == key, cancellationToken);

        if (keyExists) throw new BadRequestException("A category with this key already exists.");

        var now = _timeProvider.GetUtcNow();

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
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CategoryDtoMapper.MapToDto(category);
    }

    public async Task<CategoryDto> UpdateCategoryAsync(
        Guid id,
        UpdateCategoryDto dto,
        CancellationToken cancellationToken = default)
    {
        var category = await _dbContext.Categories
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (category is null) throw new NotFoundException($"Category '{id}' was not found.");

        var name = dto.Name.Trim();

        if (string.IsNullOrWhiteSpace(name)) throw new BadRequestException("Category name is required.");

        var nameExists = await _dbContext.Categories
            .AnyAsync(x => x.Id != id && EF.Functions.ILike(x.Name, name), cancellationToken);

        if (nameExists) throw new BadRequestException("A category with this name already exists.");

        if (dto.ParentCategoryId == id) throw new BadRequestException("A category cannot be its own parent.");

        await ValidateParentCategoryAsync(dto.ParentCategoryId, cancellationToken);

        category.Name = name;
        category.ParentCategoryId = dto.ParentCategoryId;
        category.Color = dto.Color;
        category.Icon = dto.Icon;
        category.UpdatedAt = _timeProvider.GetUtcNow();

        await _dbContext.SaveChangesAsync(cancellationToken);

        return CategoryDtoMapper.MapToDto(category);
    }

    public async Task DeleteCategoryAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var category = await _dbContext.Categories
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (category is null) throw new NotFoundException($"Category '{id}' was not found.");

        if (category.IsSystem) throw new BadRequestException("System categories cannot be deleted.");

        var now = _timeProvider.GetUtcNow();

        var transactions = await _dbContext.Transactions
            .Where(x => x.CategoryId == id)
            .ToListAsync(cancellationToken);

        foreach (var transaction in transactions)
        {
            transaction.CategoryId = null;
            transaction.UpdatedAt = now;
        }

        var childCategories = await _dbContext.Categories
            .Where(x => x.ParentCategoryId == id)
            .ToListAsync(cancellationToken);

        foreach (var childCategory in childCategories)
        {
            childCategory.ParentCategoryId = null;
            childCategory.UpdatedAt = now;
        }

        _dbContext.Categories.Remove(category);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task ValidateParentCategoryAsync(
        Guid? parentCategoryId,
        CancellationToken cancellationToken)
    {
        if (!parentCategoryId.HasValue) return;

        var parentExists = await _dbContext.Categories
            .AnyAsync(x => x.Id == parentCategoryId.Value, cancellationToken);

        if (!parentExists) throw new BadRequestException($"Parent category '{parentCategoryId}' was not found.");
    }

    private async Task<CategoryDto> ProjectCategoryByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var category = await _dbContext.Categories
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(CategoryDtoMapper.Projection)
            .FirstOrDefaultAsync(cancellationToken);

        if (category is null) throw new NotFoundException($"Category '{id}' was not found.");

        return category;
    }
}
