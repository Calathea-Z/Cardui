using Cardui.Api.Data;
using Cardui.Api.Exceptions;
using Cardui.Api.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Cardui.Tests.Services;

public class HouseholdsServiceTests
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 10, 3, 17, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetOrCreateForOwner_CreatesOneHouseholdPerOwner()
    {
        await using var dbContext = CreateDbContext();
        var service = new HouseholdsService(dbContext, new FakeTimeProvider(CreatedAt));

        var first = await service.GetOrCreateForOwnerAsync("user_owner_a");
        var again = await service.GetOrCreateForOwnerAsync("user_owner_a");
        var secondOwner = await service.GetOrCreateForOwnerAsync("user_owner_b");

        Assert.Equal(first.Id, again.Id);
        Assert.Equal(HouseholdsService.DefaultDisplayName, first.DisplayName);
        Assert.Equal(CreatedAt, first.CreatedAt);
        Assert.NotEqual(first.Id, secondOwner.Id);
        Assert.Equal(2, await dbContext.Households.CountAsync());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetOrCreateForOwner_RejectsMissingOwner(string ownerClerkUserId)
    {
        await using var dbContext = CreateDbContext();
        var service = new HouseholdsService(dbContext, new FakeTimeProvider(CreatedAt));

        await Assert.ThrowsAsync<BadRequestException>(() =>
            service.GetOrCreateForOwnerAsync(ownerClerkUserId));
    }

    private static CarduiDBContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CarduiDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CarduiDBContext(options);
    }
}
