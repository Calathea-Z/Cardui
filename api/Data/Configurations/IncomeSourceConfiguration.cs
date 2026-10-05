using Cardui.Api.Domain;
using Cardui.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cardui.Api.Data.Configurations;

public class IncomeSourceConfiguration : IEntityTypeConfiguration<IncomeSource>
{
    /// <summary>
    /// Maps an income source to its household, optional contributor, and lookup indexes.
    /// Cadence and reliability are stored as their member names.
    /// </summary>
    public void Configure(EntityTypeBuilder<IncomeSource> entity)
    {
        entity.HasKey(x => x.Id);

        entity.Property(x => x.HouseholdId)
            .IsRequired();

        entity.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(IncomeSource.NameMaxLength);

        entity.Property(x => x.TakeHomeAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        entity.Property(x => x.LowTakeHomeAmount)
            .HasPrecision(18, 2);

        entity.Property(x => x.StrongTakeHomeAmount)
            .HasPrecision(18, 2);

        entity.Property(x => x.Currency)
            .IsRequired()
            .HasMaxLength(PlanningCurrencyRules.CodeLength);

        entity.Property(x => x.Cadence)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        entity.Property(x => x.NextPaymentDate)
            .IsRequired();

        entity.Property(x => x.Reliability)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        entity.Property(x => x.CreatedAt)
            .IsRequired();

        entity.Property(x => x.UpdatedAt)
            .IsRequired();

        entity.HasOne(x => x.Household)
            .WithMany(x => x.IncomeSources)
            .HasForeignKey(x => x.HouseholdId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(x => x.Contributor)
            .WithMany()
            .HasForeignKey(x => x.ContributorId)
            .OnDelete(DeleteBehavior.SetNull);

        entity.HasIndex(x => new { x.HouseholdId, x.Name });

        entity.HasIndex(x => x.ContributorId);
    }
}
