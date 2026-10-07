using Cardui.Api.Data;
using Cardui.Api.Domain.Categories;
using Cardui.Api.Dtos.SubGroup;
using Cardui.Api.Exceptions;
using Cardui.Api.Mapping;
using Cardui.Api.Models;
using Cardui.Api.Security;
using Cardui.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cardui.Api.Services.Implementations;

public class SubGroupsService : ISubGroupsService
{
    private readonly CarduiDBContext _dbContext;
    private readonly TimeProvider _timeProvider;
    private readonly HouseholdScope _householdScope;

    public SubGroupsService(
        CarduiDBContext dbContext,
        TimeProvider timeProvider,
        HouseholdScope householdScope)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
        _householdScope = householdScope;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SubGroupDto>> GetSubGroupsAsync(
        Guid? groupId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.SubGroups
            .AsNoTracking()
            .VisibleToHousehold(_householdScope);

        if (groupId.HasValue)
        {
            query = query.Where(x => x.GroupId == groupId.Value);
        }

        return await query
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(SubGroupDtoMapper.Projection)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<SubGroupDto> GetSubGroupByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        ProjectSubGroupByIdAsync(id, cancellationToken);

    /// <inheritdoc />
    public async Task<SubGroupDto> CreateSubGroupAsync(
        CreateSubGroupDto dto,
        CancellationToken cancellationToken = default)
    {
        var name = dto.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new BadRequestException("Sub-group name is required.");
        }

        var groupExists = await _dbContext.Groups
            .AnyAsync(x => x.Id == dto.GroupId, cancellationToken);

        if (!groupExists)
        {
            throw new BadRequestException($"Group '{dto.GroupId}' was not found.");
        }

        var nameExists = await NamesInHousehold(dto.GroupId)
            .AnyAsync(x => EF.Functions.ILike(x.Name, name), cancellationToken);

        if (nameExists)
        {
            throw new BadRequestException("A sub-group with this name already exists in the group.");
        }

        var key = CategoryKeys.CreateFromName(name);
        var keyExists = await KeysInHousehold()
            .AnyAsync(x => x.Key == key, cancellationToken);

        if (keyExists)
        {
            throw new BadRequestException("A sub-group with this key already exists.");
        }

        var maxSortOrder = await _dbContext.SubGroups
            .Where(x => x.GroupId == dto.GroupId)
            .Select(x => (int?)x.SortOrder)
            .MaxAsync(cancellationToken) ?? -1;

        var now = _timeProvider.GetUtcNow();
        var subGroup = new SubGroup
        {
            Id = Guid.NewGuid(),
            GroupId = dto.GroupId,
            Key = key,
            Name = name,
            IsSystem = false,
            HouseholdId = _householdScope.RequireHouseholdId(),
            SortOrder = maxSortOrder + 1,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.SubGroups.Add(subGroup);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return SubGroupDtoMapper.MapToDto(subGroup);
    }

    /// <inheritdoc />
    public async Task<SubGroupDto> UpdateSubGroupAsync(
        Guid id,
        UpdateSubGroupDto dto,
        CancellationToken cancellationToken = default)
    {
        var subGroup = await _dbContext.SubGroups
            .VisibleToHousehold(_householdScope)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (subGroup is null)
        {
            throw new NotFoundException($"Sub-group '{id}' was not found.");
        }

        if (subGroup.IsSystem)
        {
            throw new BadRequestException("System sub-groups cannot be renamed.");
        }

        var name = dto.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new BadRequestException("Sub-group name is required.");
        }

        var nameExists = await NamesInHousehold(subGroup.GroupId)
            .AnyAsync(
                x => x.Id != id && EF.Functions.ILike(x.Name, name),
                cancellationToken);

        if (nameExists)
        {
            throw new BadRequestException("A sub-group with this name already exists in the group.");
        }

        subGroup.Name = name;
        subGroup.UpdatedAt = _timeProvider.GetUtcNow();

        await _dbContext.SaveChangesAsync(cancellationToken);

        return SubGroupDtoMapper.MapToDto(subGroup);
    }

    /// <inheritdoc />
    public async Task DeleteSubGroupAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var subGroup = await _dbContext.SubGroups
            .VisibleToHousehold(_householdScope)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (subGroup is null)
        {
            throw new NotFoundException($"Sub-group '{id}' was not found.");
        }

        if (subGroup.IsSystem)
        {
            throw new BadRequestException("System sub-groups cannot be deleted.");
        }

        var householdId = _householdScope.RequireHouseholdId();
        var hasCategories = await _dbContext.Categories
            .AnyAsync(
                x => x.SubGroupId == id
                    && (x.HouseholdId == null || x.HouseholdId == householdId || x.IsSystem),
                cancellationToken);

        if (hasCategories)
        {
            throw new BadRequestException(
                "Sub-group cannot be deleted while it still has categories.");
        }

        _dbContext.SubGroups.Remove(subGroup);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    #region Private Methods

    /// <summary>
    /// Keeps system sub-groups and this household's sub-groups in one group.
    /// </summary>
    private IQueryable<SubGroup> NamesInHousehold(Guid groupId)
    {
        var householdId = _householdScope.RequireHouseholdId();
        return _dbContext.SubGroups.Where(subGroup =>
            subGroup.GroupId == groupId
            && (subGroup.IsSystem || subGroup.HouseholdId == householdId));
    }

    /// <summary>
    /// Keeps system sub-group keys and this household's sub-group keys.
    /// </summary>
    private IQueryable<SubGroup> KeysInHousehold()
    {
        var householdId = _householdScope.RequireHouseholdId();
        return _dbContext.SubGroups.Where(subGroup =>
            subGroup.IsSystem || subGroup.HouseholdId == householdId);
    }

    /// <summary>
    /// Loads one visible sub-group, or throws when it is missing.
    /// </summary>
    private async Task<SubGroupDto> ProjectSubGroupByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var subGroup = await _dbContext.SubGroups
            .AsNoTracking()
            .VisibleToHousehold(_householdScope)
            .Where(x => x.Id == id)
            .Select(SubGroupDtoMapper.Projection)
            .FirstOrDefaultAsync(cancellationToken);

        if (subGroup is null)
        {
            throw new NotFoundException($"Sub-group '{id}' was not found.");
        }

        return subGroup;
    }

    #endregion
}
