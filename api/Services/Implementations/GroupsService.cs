using Cardui.Api.Data;
using Cardui.Api.Dtos.Group;
using Cardui.Api.Exceptions;
using Cardui.Api.Mapping;
using Cardui.Api.Security;
using Cardui.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cardui.Api.Services.Implementations;

public class GroupsService : IGroupsService
{
    private readonly CarduiDBContext _dbContext;
    private readonly HouseholdScope _householdScope;

    public GroupsService(CarduiDBContext dbContext, HouseholdScope householdScope)
    {
        _dbContext = dbContext;
        _householdScope = householdScope;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<GroupDto>> GetGroupsAsync(
        CancellationToken cancellationToken = default)
    {
        _householdScope.EnsureBound();

        return await _dbContext.Groups
            .AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(GroupDtoMapper.Projection)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<GroupDetailDto> GetGroupByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        _householdScope.EnsureBound();

        var group = await _dbContext.Groups
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (group is null)
        {
            throw new NotFoundException($"Group '{id}' was not found.");
        }

        var subGroups = await _dbContext.SubGroups
            .AsNoTracking()
            .VisibleToHousehold(_householdScope)
            .Where(subGroup => subGroup.GroupId == id)
            .ToListAsync(cancellationToken);

        return GroupDtoMapper.MapToDetailDto(group, subGroups);
    }
}
