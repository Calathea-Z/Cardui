using Cardui.Api.Data;
using Cardui.Api.Domain;
using Cardui.Api.Models;
using Cardui.Api.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Cardui.Tests.Services;

public class TransactionCategorizationServiceTests
{
    private static readonly DateTimeOffset SeededAt =
        new(2026, 7, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetSystemCategoryIdsByKeyAsync_OmitsCustomCategories()
    {
        await using var dbContext = CreateDbContext();
        var shoppingId = Guid.NewGuid();
        var customId = Guid.NewGuid();
        var subGroupId = Guid.NewGuid();

        dbContext.Categories.AddRange(
            Category(shoppingId, subGroupId, SystemCategoryKeys.Shopping, "Shopping", isSystem: true, householdId: null),
            Category(customId, subGroupId, "pet-care", "Pet Care", isSystem: false, householdId: Guid.NewGuid()));
        await dbContext.SaveChangesAsync();

        var service = new TransactionCategorizationService(dbContext);
        var ids = await service.GetSystemCategoryIdsByKeyAsync();

        Assert.Equal(shoppingId, ids[SystemCategoryKeys.Shopping]);
        Assert.False(ids.ContainsKey("pet-care"));
    }

    [Fact]
    public void FindCategoryId_ReturnsTheLoadedIdForAKeywordMatch()
    {
        var shoppingId = Guid.NewGuid();
        using var dbContext = CreateDbContext();
        var service = new TransactionCategorizationService(dbContext);
        var categoryId = service.FindCategoryId(
            new Dictionary<string, Guid>
            {
                [SystemCategoryKeys.Shopping] = shoppingId
            },
            name: "Amazon",
            merchantName: "Amazon",
            amount: 25m);

        Assert.Equal(shoppingId, categoryId);
    }

    [Fact]
    public void FindCategoryId_ReturnsNullWhenTheCategoryIsNotSeeded()
    {
        using var dbContext = CreateDbContext();
        var service = new TransactionCategorizationService(dbContext);
        var categoryId = service.FindCategoryId(
            new Dictionary<string, Guid>(),
            name: "Card credit",
            merchantName: null,
            amount: -10m);

        Assert.Null(categoryId);
    }

    private static CarduiDBContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CarduiDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CarduiDBContext(options);
    }

    private static Category Category(
        Guid id,
        Guid subGroupId,
        string key,
        string name,
        bool isSystem,
        Guid? householdId)
    {
        return new Category
        {
            Id = id,
            SubGroupId = subGroupId,
            Key = key,
            Name = name,
            IsSystem = isSystem,
            HouseholdId = householdId,
            CreatedAt = SeededAt,
            UpdatedAt = SeededAt
        };
    }
}
