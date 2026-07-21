using Cardui.Api.Data;
using Cardui.Api.Dtos.Group;
using Cardui.Api.Exceptions;
using Cardui.Api.Mapping;
using Cardui.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cardui.Api.Services.Implementations;

public class GroupsService : IGroupsService
{
    private readonly CarduiDBContext _dbContext;

    public GroupsService(CarduiDBContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<GroupDto>> GetGroupsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Groups
            .AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(GroupDtoMapper.Projection)
            .ToListAsync(cancellationToken);
    }

    public async Task<GroupDetailDto> GetGroupByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var group = await _dbContext.Groups
            .AsNoTracking()
            .Include(x => x.SubGroups)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (group is null)
        {
            throw new NotFoundException($"Group '{id}' was not found.");
        }

        return GroupDtoMapper.MapToDetailDto(group);
    }
}
