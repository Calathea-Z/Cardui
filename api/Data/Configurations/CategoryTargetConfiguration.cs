using Cardui.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cardui.Api.Data.Configurations;

public class CategoryTargetConfiguration : IEntityTypeConfiguration<CategoryTarget>
{
    /// <summary>
    /// Maps one category's target for a started month.
    /// The month and category are unique. Money is stored in cents.
    /// Deleting the month or the category deletes the target.
    /// </summary>
    public void Configure(EntityTypeBuilder<CategoryTarget> entity)
    {
        entity.HasKey(x => x.Id);

        entity.Property(x => x.CategoryTargetMonthId)
            .IsRequired();

        entity.Property(x => x.CategoryId)
            .IsRequired();

        entity.Property(x => x.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        entity.Property(x => x.Rollover)
            .IsRequired();

        entity.Property(x => x.CreatedAt)
            .IsRequired();

        entity.Property(x => x.UpdatedAt)
            .IsRequired();

        entity.HasOne(x => x.Month)
            .WithMany(x => x.Targets)
            .HasForeignKey(x => x.CategoryTargetMonthId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(x => x.Category)
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasIndex(x => new { x.CategoryTargetMonthId, x.CategoryId })
            .IsUnique();

        entity.HasIndex(x => x.CategoryId);
    }
}
