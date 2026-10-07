using Cardui.Api.Data;
using Cardui.Api.Domain.Categories;
using Cardui.Api.Dtos.Category;
using Cardui.Api.Exceptions;
using Cardui.Api.Mapping;
using Cardui.Api.Models;
using Cardui.Api.Security;
using Cardui.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cardui.Api.Services.Implementations;

public class CategoriesService : ICategoriesService
{
    private readonly CarduiDBContext _dbContext;
    private readonly TimeProvider _timeProvider;
    private readonly HouseholdScope _householdScope;

    public CategoriesService(
        CarduiDBContext dbContext,
        TimeProvider timeProvider,
        HouseholdScope householdScope)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
        _householdScope = householdScope;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Categories
            .AsNoTracking()
            .VisibleToHousehold(_householdScope)
            .OrderBy(x => x.Name)
            .Select(CategoryDtoMapper.Projection)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<CategoryDto> GetCategoryByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return ProjectCategoryByIdAsync(id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<CategoryDto> CreateCategoryAsync(
        CreateCategoryDto createCategoryDto,
        CancellationToken cancellationToken = default)
    {
        var name = createCategoryDto.Name.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new BadRequestException("Category name is required.");
        }

        var nameExists = await NamesInHousehold()
            .AnyAsync(x => EF.Functions.ILike(x.Name, name), cancellationToken);

        if (nameExists)
        {
            throw new BadRequestException("A category with this name already exists.");
        }

        await ValidateSubGroupAsync(createCategoryDto.SubGroupId, cancellationToken);

        var key = CategoryKeys.CreateFromName(name);

        var keyExists = await NamesInHousehold()
            .AnyAsync(x => x.Key == key, cancellationToken);

        if (keyExists)
        {
            throw new BadRequestException("A category with this key already exists.");
        }

        var now = _timeProvider.GetUtcNow();

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = name,
            Key = key,
            SubGroupId = createCategoryDto.SubGroupId,
            Color = createCategoryDto.Color,
            Icon = createCategoryDto.Icon,
            IsSystem = false,
            HouseholdId = _householdScope.RequireHouseholdId(),
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.Categories.Add(category);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CategoryDtoMapper.MapToDto(category);
    }

    /// <inheritdoc />
    public async Task<CategoryDto> UpdateCategoryAsync(
        Guid id,
        UpdateCategoryDto dto,
        CancellationToken cancellationToken = default)
    {
        var category = await _dbContext.Categories
            .VisibleToHousehold(_householdScope)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (category is null)
        {
            throw new NotFoundException($"Category '{id}' was not found.");
        }

        if (category.IsSystem)
        {
            throw new BadRequestException("System categories cannot be changed.");
        }

        var name = dto.Name.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new BadRequestException("Category name is required.");
        }

        var nameExists = await NamesInHousehold()
            .AnyAsync(x => x.Id != id && EF.Functions.ILike(x.Name, name), cancellationToken);

        if (nameExists)
        {
            throw new BadRequestException("A category with this name already exists.");
        }

        await ValidateSubGroupAsync(dto.SubGroupId, cancellationToken);

        category.Name = name;
        category.SubGroupId = dto.SubGroupId;
        category.Color = dto.Color;
        category.Icon = dto.Icon;
        category.UpdatedAt = _timeProvider.GetUtcNow();

        await _dbContext.SaveChangesAsync(cancellationToken);

        return CategoryDtoMapper.MapToDto(category);
    }

    /// <inheritdoc />
    public async Task DeleteCategoryAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var category = await _dbContext.Categories
            .VisibleToHousehold(_householdScope)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (category is null)
        {
            throw new NotFoundException($"Category '{id}' was not found.");
        }

        if (category.IsSystem)
        {
            throw new BadRequestException("System categories cannot be deleted.");
        }

        var now = _timeProvider.GetUtcNow();

        var transactions = await _dbContext.Transactions
            .InHousehold(_dbContext, _householdScope)
            .Where(x => x.CategoryId == id)
            .ToListAsync(cancellationToken);

        foreach (var transaction in transactions)
        {
            transaction.CategoryId = null;
            transaction.UpdatedAt = now;
        }

        _dbContext.Categories.Remove(category);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    #region Private Methods

    /// <summary>
    /// Keeps system categories and categories owned by this household.
    /// Another household's custom names are excluded.
    /// </summary>
    private IQueryable<Category> NamesInHousehold()
    {
        var householdId = _householdScope.RequireHouseholdId();
        return _dbContext.Categories.Where(category =>
            category.IsSystem || category.HouseholdId == householdId);
    }

    /// <summary>
    /// Rejects a sub-group the household cannot see.
    /// </summary>
    private async Task ValidateSubGroupAsync(
        Guid subGroupId,
        CancellationToken cancellationToken)
    {
        var subGroupExists = await _dbContext.SubGroups
            .VisibleToHousehold(_householdScope)
            .AnyAsync(x => x.Id == subGroupId, cancellationToken);

        if (!subGroupExists)
        {
            throw new BadRequestException($"Sub-group '{subGroupId}' was not found.");
        }
    }

    /// <summary>
    /// Loads one visible category, or throws when it is missing.
    /// </summary>
    private async Task<CategoryDto> ProjectCategoryByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var category = await _dbContext.Categories
            .AsNoTracking()
            .VisibleToHousehold(_householdScope)
            .Where(x => x.Id == id)
            .Select(CategoryDtoMapper.Projection)
            .FirstOrDefaultAsync(cancellationToken);

        if (category is null)
        {
            throw new NotFoundException($"Category '{id}' was not found.");
        }

        return category;
    }

    #endregion
}
