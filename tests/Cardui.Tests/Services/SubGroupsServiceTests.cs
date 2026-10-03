using Cardui.Api.Data;
using Cardui.Api.Dtos.SubGroup;
using Cardui.Api.Exceptions;
using Cardui.Api.Models;
using Cardui.Api.Security;
using Cardui.Api.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Cardui.Tests.Services;

public class SubGroupsServiceTests
{
    private static readonly DateTimeOffset SeededAt =
        new(2026, 7, 1, 12, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset UpdatedAt =
        new(2026, 7, 20, 18, 30, 0, TimeSpan.Zero);

    private static readonly Guid TestHouseholdId =
        Guid.Parse("33333333-3333-3333-3333-333333333333");

    [Fact]
    public async Task CreateSubGroup_CreatesUnderGroup()
    {
        await using var dbContext = CreateDbContext();
        var groupId = await SeedGroupAsync(dbContext);
        var service = CreateService(dbContext);

        var result = await service.CreateSubGroupAsync(new CreateSubGroupDto
        {
            GroupId = groupId,
            Name = "Custom Bucket"
        });

        Assert.Equal(groupId, result.GroupId);
        Assert.Equal("Custom Bucket", result.Name);
        Assert.Equal("custom-bucket", result.Key);
        Assert.False(result.IsSystem);
    }

    [Fact]
    public async Task DeleteSubGroup_RejectsSystemSubGroup()
    {
        await using var dbContext = CreateDbContext();
        var (_, subGroupId) = await SeedSystemSubGroupAsync(dbContext);
        var service = CreateService(dbContext);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            service.DeleteSubGroupAsync(subGroupId));
    }

    [Fact]
    public async Task DeleteSubGroup_RejectsWhenCategoriesExist()
    {
        await using var dbContext = CreateDbContext();
        var groupId = await SeedGroupAsync(dbContext);
        var customSubGroupId = Guid.NewGuid();

        dbContext.SubGroups.Add(new SubGroup
        {
            Id = customSubGroupId,
            GroupId = groupId,
            Key = "temporary",
            Name = "Temporary",
            IsSystem = false,
            HouseholdId = TestHouseholdId,
            SortOrder = 1,
            CreatedAt = SeededAt,
            UpdatedAt = SeededAt
        });

        dbContext.Categories.Add(new Category
        {
            Id = Guid.NewGuid(),
            SubGroupId = customSubGroupId,
            Key = "temporary-cat",
            Name = "Temporary Cat",
            IsSystem = false,
            CreatedAt = SeededAt,
            UpdatedAt = SeededAt
        });
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            service.DeleteSubGroupAsync(customSubGroupId));
    }

    private static CarduiDBContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CarduiDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CarduiDBContext(options);
    }

    private static SubGroupsService CreateService(CarduiDBContext dbContext)
    {
        var scope = new HouseholdScope();
        scope.Bind(TestHouseholdId);
        return new SubGroupsService(dbContext, new FakeTimeProvider(UpdatedAt), scope);
    }

    private static async Task<Guid> SeedGroupAsync(CarduiDBContext dbContext)
    {
        var groupId = Guid.NewGuid();
        dbContext.Groups.Add(new Group
        {
            Id = groupId,
            Key = "expenses",
            Name = "Expenses",
            SortOrder = 1,
            CreatedAt = SeededAt,
            UpdatedAt = SeededAt
        });
        await dbContext.SaveChangesAsync();
        return groupId;
    }

    private static async Task<(Guid GroupId, Guid SubGroupId)> SeedSystemSubGroupAsync(
        CarduiDBContext dbContext)
    {
        var groupId = await SeedGroupAsync(dbContext);
        var subGroupId = Guid.NewGuid();

        dbContext.SubGroups.Add(new SubGroup
        {
            Id = subGroupId,
            GroupId = groupId,
            Key = "shopping",
            Name = "Shopping",
            IsSystem = true,
            SortOrder = 0,
            CreatedAt = SeededAt,
            UpdatedAt = SeededAt
        });
        await dbContext.SaveChangesAsync();

        return (groupId, subGroupId);
    }
}
