using Cardui.Api.Domain;
using Cardui.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Cardui.Api.Data;

public static class DataSeeder
{
    /// <summary>
    /// Inserts the system groups, sub-groups, and categories when none exist.
    /// </summary>
    public static async Task SeedAsync(
        CarduiDBContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken = default)
    {
        if (await dbContext.Groups.AnyAsync(cancellationToken))
        {
            return;
        }

        var now = timeProvider.GetUtcNow();

        var incomeGroup = CreateGroup("Income", SystemGroupKeys.Income, sortOrder: 0, now);
        var expensesGroup = CreateGroup("Expenses", SystemGroupKeys.Expenses, sortOrder: 1, now);
        var transfersGroup = CreateGroup("Transfers", SystemGroupKeys.Transfers, sortOrder: 2, now);

        dbContext.Groups.AddRange(incomeGroup, expensesGroup, transfersGroup);

        var subGroupDefs = new (Group Group, string Name, int SortOrder)[]
        {
            (incomeGroup, "Income", 0),
            (expensesGroup, "Gifts & Donations", 0),
            (expensesGroup, "Auto & Transport", 1),
            (expensesGroup, "Housing", 2),
            (expensesGroup, "Bills & Utilities", 3),
            (expensesGroup, "Food & Dining", 4),
            (expensesGroup, "Travel & Lifestyle", 5),
            (expensesGroup, "Shopping", 6),
            (expensesGroup, "Children", 7),
            (expensesGroup, "Education", 8),
            (expensesGroup, "Health & Wellness", 9),
            (expensesGroup, "Financial", 10),
            (expensesGroup, "Other", 11),
            (expensesGroup, "Business", 12),
            (transfersGroup, "Transfers", 0),
        };

        var categoryColors = new Dictionary<string, (string Color, string Icon)>(StringComparer.Ordinal)
        {
            [SystemCategoryKeys.Income] = ("#16a34a", "💰"),
            [SystemCategoryKeys.Transfers] = ("#64748b", "↔️"),
            [SystemCategoryKeys.FoodDining] = ("#f97316", "🍽️"),
            [SystemCategoryKeys.AutoTransport] = ("#0ea5e9", "🚗"),
            [SystemCategoryKeys.BillsUtilities] = ("#6366f1", "🧾"),
            [SystemCategoryKeys.Shopping] = ("#ec4899", "🛍️"),
            [SystemCategoryKeys.TravelLifestyle] = ("#a855f7", "🎬"),
            [SystemCategoryKeys.Other] = ("#71717a", "📦"),
        };

        foreach (var (group, name, sortOrder) in subGroupDefs)
        {
            var key = CategoryKeys.CreateFromName(name);
            var subGroup = new SubGroup
            {
                Id = Guid.NewGuid(),
                GroupId = group.Id,
                Key = key,
                Name = name,
                IsSystem = true,
                SortOrder = sortOrder,
                CreatedAt = now,
                UpdatedAt = now
            };

            dbContext.SubGroups.Add(subGroup);

            categoryColors.TryGetValue(key, out var style);

            dbContext.Categories.Add(new Category
            {
                Id = Guid.NewGuid(),
                SubGroupId = subGroup.Id,
                Key = key,
                Name = name,
                Color = style.Color ?? "#71717a",
                Icon = style.Icon ?? "❔",
                IsSystem = true,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    #region Private Methods

    /// <summary>
    /// Creates a system group row that has not been saved yet.
    /// </summary>
    private static Group CreateGroup(
        string name,
        string key,
        int sortOrder,
        DateTimeOffset now)
    {
        return new Group
        {
            Id = Guid.NewGuid(),
            Name = name,
            Key = key,
            SortOrder = sortOrder,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    #endregion
}
