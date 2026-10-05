using Cardui.Api.Domain;
using Cardui.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cardui.Api.Data.Configurations;

public class ObligationConfiguration : IEntityTypeConfiguration<Obligation>
{
    /// <summary>
    /// Maps a bill to its household, optional source account, and lookup indexes.
    /// Cadence and flexibility are stored as their member names.
    /// SuggestionKey is set when the bill was added from a recurring pattern.
    /// Deleting the household deletes the bill. Deleting the account clears the link.
    /// </summary>
    public void Configure(EntityTypeBuilder<Obligation> entity)
    {
        entity.HasKey(x => x.Id);

        entity.Property(x => x.HouseholdId)
            .IsRequired();

        entity.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(Obligation.NameMaxLength);

        entity.Property(x => x.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        entity.Property(x => x.Currency)
            .IsRequired()
            .HasMaxLength(PlanningCurrencyRules.CodeLength);

        entity.Property(x => x.Cadence)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        entity.Property(x => x.NextDueDate)
            .IsRequired();

        entity.Property(x => x.Flexibility)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        entity.Property(x => x.SuggestionKey)
            .HasMaxLength(ObligationSuggestionDismissal.KeyMaxLength);

        entity.Property(x => x.CreatedAt)
            .IsRequired();

        entity.Property(x => x.UpdatedAt)
            .IsRequired();

        entity.HasOne(x => x.Household)
            .WithMany(x => x.Obligations)
            .HasForeignKey(x => x.HouseholdId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(x => x.Account)
            .WithMany()
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.SetNull);

        entity.HasIndex(x => new { x.HouseholdId, x.Name });

        entity.HasIndex(x => x.AccountId);
    }
}
