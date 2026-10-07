using Cardui.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cardui.Api.Data.Configurations;

public class CategoryTargetMonthConfiguration : IEntityTypeConfiguration<CategoryTargetMonth>
{
    /// <summary>
    /// Maps one started month of category targets for a household.
    /// The household, year, and month are unique. Deleting the household deletes the month.
    /// </summary>
    public void Configure(EntityTypeBuilder<CategoryTargetMonth> entity)
    {
        entity.HasKey(x => x.Id);

        entity.Property(x => x.HouseholdId)
            .IsRequired();

        entity.Property(x => x.Year)
            .IsRequired();

        entity.Property(x => x.Month)
            .IsRequired();

        entity.Property(x => x.CreatedAt)
            .IsRequired();

        entity.HasOne(x => x.Household)
            .WithMany(x => x.CategoryTargetMonths)
            .HasForeignKey(x => x.HouseholdId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasIndex(x => new { x.HouseholdId, x.Year, x.Month })
            .IsUnique();
    }
}
