using Cardui.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cardui.Api.Data.Configurations;

public class AllocationPreferenceConfiguration : IEntityTypeConfiguration<AllocationPreference>
{
    /// <summary>
    /// Maps one allocation preference per household.
    /// Deleting the household deletes the preference. The percents default to zero,
    /// which leaves the whole amount available.
    /// </summary>
    public void Configure(EntityTypeBuilder<AllocationPreference> entity)
    {
        entity.HasKey(x => x.HouseholdId);

        entity.Property(x => x.VariableIncomeDebtPercent)
            .IsRequired()
            .HasDefaultValue(0);

        entity.Property(x => x.VariableIncomeSavingsPercent)
            .IsRequired()
            .HasDefaultValue(0);

        entity.Property(x => x.WindfallDebtPercent)
            .IsRequired()
            .HasDefaultValue(0);

        entity.Property(x => x.WindfallSavingsPercent)
            .IsRequired()
            .HasDefaultValue(0);

        entity.Property(x => x.UpdatedAt)
            .IsRequired();

        entity.HasOne(x => x.Household)
            .WithOne(x => x.AllocationPreference)
            .HasForeignKey<AllocationPreference>(x => x.HouseholdId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
