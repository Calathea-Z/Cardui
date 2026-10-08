using Cardui.Api.Domain;
using Cardui.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cardui.Api.Data.Configurations;

public class SavingsGoalConfiguration : IEntityTypeConfiguration<SavingsGoal>
{
    /// <summary>
    /// Maps a savings goal to its household and optional cash account.
    /// Kind is stored as its member name. One household has one operating reserve and one emergency goal.
    /// One followed account backs one goal. Deleting the household deletes the goal. Deleting the account clears the link.
    /// A second index on the same column replaces the first unless it has its own name, so the operating and lookup indexes are named.
    /// The emergency index stays unnamed and first, matching the index already created by AddSavingsGoals.
    /// </summary>
    public void Configure(EntityTypeBuilder<SavingsGoal> entity)
    {
        entity.HasKey(x => x.Id);

        entity.Property(x => x.HouseholdId)
            .IsRequired();

        entity.Property(x => x.Kind)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        entity.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(SavingsGoal.NameMaxLength);

        entity.Property(x => x.TargetAmount)
            .HasPrecision(18, 2);

        entity.Property(x => x.MonthlyAmount)
            .HasPrecision(18, 2);

        entity.Property(x => x.FloorAmount)
            .HasPrecision(18, 2);

        entity.Property(x => x.ReservedAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        entity.Property(x => x.Currency)
            .IsRequired()
            .HasMaxLength(PlanningCurrencyRules.CodeLength);

        entity.Property(x => x.CreatedAt)
            .IsRequired();

        entity.Property(x => x.UpdatedAt)
            .IsRequired();

        entity.HasOne(x => x.Household)
            .WithMany(x => x.SavingsGoals)
            .HasForeignKey(x => x.HouseholdId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(x => x.Account)
            .WithMany()
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.SetNull);

        entity.HasIndex(x => x.HouseholdId)
            .IsUnique()
            .HasFilter("\"Kind\" = 'Emergency'")
            .HasDatabaseName("IX_SavingsGoals_HouseholdId_Emergency");

        entity.HasIndex(x => x.HouseholdId, "IX_SavingsGoals_HouseholdId_Operating")
            .IsUnique()
            .HasFilter("\"Kind\" = 'Operating'");

        entity.HasIndex(x => x.HouseholdId, "IX_SavingsGoals_HouseholdId_Floor")
            .IsUnique()
            .HasFilter("\"Kind\" = 'Floor'");

        entity.HasIndex(x => x.HouseholdId, "IX_SavingsGoals_HouseholdId_Lookup")
            .HasDatabaseName("IX_SavingsGoals_HouseholdId");

        entity.HasIndex(x => x.AccountId)
            .IsUnique()
            .HasFilter("\"AccountFollowedSince\" IS NOT NULL")
            .HasDatabaseName("IX_SavingsGoals_AccountId_Followed");
    }
}
