using Cardui.Api.Domain;
using Cardui.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cardui.Api.Data.Configurations;

public class PlanVersionConfiguration : IEntityTypeConfiguration<PlanVersion>
{
    /// <summary>
    /// Maps one saved plan version to its household.
    /// Status is stored as its member name. The document is JSON.
    /// Deleting the household deletes the versions.
    /// One household has at most one draft and one accepted plan. Many superseded versions are allowed.
    /// Version numbers are unique inside the household.
    /// </summary>
    public void Configure(EntityTypeBuilder<PlanVersion> entity)
    {
        entity.HasKey(x => x.Id);

        entity.Property(x => x.HouseholdId)
            .IsRequired();

        entity.Property(x => x.Number)
            .IsRequired();

        entity.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        entity.Property(x => x.CreatedAt)
            .IsRequired();

        entity.Property(x => x.CalculationVersion)
            .IsRequired()
            .HasMaxLength(PlanVersion.CalculationVersionMaxLength);

        entity.Property(x => x.AsOf)
            .IsRequired();

        entity.Property(x => x.PlanningCurrency)
            .IsRequired()
            .HasMaxLength(PlanningCurrencyRules.CodeLength);

        entity.Property(x => x.Document)
            .IsRequired()
            .HasColumnType("jsonb");

        entity.HasIndex(x => new { x.HouseholdId, x.Number })
            .IsUnique();

        entity.HasIndex(x => x.HouseholdId, "IX_PlanVersions_HouseholdId_Draft")
            .IsUnique()
            .HasFilter("\"Status\" = 'Draft'");

        entity.HasIndex(x => x.HouseholdId, "IX_PlanVersions_HouseholdId_Accepted")
            .IsUnique()
            .HasFilter("\"Status\" = 'Accepted'");

        entity.HasOne(x => x.Household)
            .WithMany(x => x.PlanVersions)
            .HasForeignKey(x => x.HouseholdId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
