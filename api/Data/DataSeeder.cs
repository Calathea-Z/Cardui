using Cardui.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Cardui.Api.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(CarduiDBContext dbContext)
    {
        if (await dbContext.Categories.AnyAsync())
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;

        var categories = new List<Category>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Income",
                Color = "#16a34a",
                Icon = "banknote",
                IsSystem = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Groceries",
                Color = "#22c55e",
                Icon = "shopping-basket",
                IsSystem = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Dining",
                Color = "#f97316",
                Icon = "utensils",
                IsSystem = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Bills",
                Color = "#6366f1",
                Icon = "receipt",
                IsSystem = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Transport",
                Color = "#0ea5e9",
                Icon = "car",
                IsSystem = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Shopping",
                Color = "#ec4899",
                Icon = "shopping-bag",
                IsSystem = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Entertainment",
                Color = "#a855f7",
                Icon = "ticket",
                IsSystem = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Transfers",
                Color = "#64748b",
                Icon = "repeat",
                IsSystem = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Uncategorized",
                Color = "#71717a",
                Icon = "circle-help",
                IsSystem = true,
                CreatedAt = now,
                UpdatedAt = now
            }
        };

        dbContext.Categories.AddRange(categories);
        await dbContext.SaveChangesAsync();
    }
}