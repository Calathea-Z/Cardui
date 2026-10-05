using Cardui.Api.Domain;
using Cardui.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cardui.Api.Data.Configurations;

public class DebtConfiguration : IEntityTypeConfiguration<Debt>
{
    /// <summary>
    /// Maps a debt to its household, optional linked account, and lookup indexes.
    /// Kind is stored as its member name. Rates use three decimal places. Money uses cents.
    /// A null term is unknown. Deleting the household deletes the debt. Deleting the account clears the link.
    /// </summary>
    public void Configure(EntityTypeBuilder<Debt> entity)
    {
        entity.HasKey(x => x.Id);

        entity.Property(x => x.HouseholdId)
            .IsRequired();

        entity.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(Debt.NameMaxLength);

        entity.Property(x => x.Kind)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        entity.Property(x => x.Balance)
            .HasPrecision(18, 2);

        entity.Property(x => x.Currency)
            .IsRequired()
            .HasMaxLength(PlanningCurrencyRules.CodeLength);

        entity.Property(x => x.Apr)
            .HasPrecision(6, 3);

        entity.Property(x => x.MinimumPayment)
            .HasPrecision(18, 2);

        entity.Property(x => x.CreditLimit)
            .HasPrecision(18, 2);

        entity.Property(x => x.PromotionalApr)
            .HasPrecision(6, 3);

        entity.Property(x => x.CreatedAt)
            .IsRequired();

        entity.Property(x => x.UpdatedAt)
            .IsRequired();

        entity.HasOne(x => x.Household)
            .WithMany(x => x.Debts)
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
