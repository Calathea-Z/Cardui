using Cardui.Api.Domain;
using Cardui.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Cardui.Api.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(
        CarduiDBContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken = default)
    {
        if (await dbContext.Categories.AnyAsync(cancellationToken)) return;

        var now = timeProvider.GetUtcNow();

        var categories = new List<Category>
        {
            CreateSystemCategory("Income", SystemCategoryKeys.Income, "#16a34a", "💰", now),
            CreateSystemCategory("Groceries", SystemCategoryKeys.Groceries, "#22c55e", "🛒", now),
            CreateSystemCategory("Dining", SystemCategoryKeys.Dining, "#f97316", "🍽️", now),
            CreateSystemCategory("Bills", SystemCategoryKeys.Bills, "#6366f1", "🧾", now),
            CreateSystemCategory("Transport", SystemCategoryKeys.Transport, "#0ea5e9", "🚗", now),
            CreateSystemCategory("Shopping", SystemCategoryKeys.Shopping, "#ec4899", "🛍️", now),
            CreateSystemCategory("Entertainment", SystemCategoryKeys.Entertainment, "#a855f7", "🎬", now),
            CreateSystemCategory("Transfers", SystemCategoryKeys.Transfers, "#64748b", "↔️", now),
            CreateSystemCategory(SystemCategoryNames.Uncategorized, SystemCategoryKeys.Uncategorized, "#71717a", "❔", now)
        };

        dbContext.Categories.AddRange(categories);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static Category CreateSystemCategory(
        string name,
        string key,
        string color,
        string icon,
        DateTimeOffset now)
    {
        return new Category
        {
            Id = Guid.NewGuid(),
            Name = name,
            Key = key,
            Color = color,
            Icon = icon,
            IsSystem = true,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
