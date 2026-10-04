using Cardui.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cardui.Api.Data.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    /// <summary>
    /// Maps transaction columns, lookup indexes, and the optional category.
    /// </summary>
    public void Configure(EntityTypeBuilder<Transaction> entity)
    {
        entity.HasKey(x => x.Id);

        entity.Property(x => x.PlaidTransactionId)
            .HasMaxLength(200);

        entity.Property(x => x.Source)
            .IsRequired()
            .HasMaxLength(FinancialRecordSource.MaxLength);

        entity.Property(x => x.Provenance)
            .IsRequired()
            .HasMaxLength(FinancialRecordProvenance.MaxLength);

        entity.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(300);

        entity.Property(x => x.MerchantName)
            .HasMaxLength(300);

        entity.Property(x => x.Amount)
            .HasPrecision(18, 2);

        entity.Property(x => x.IsoCurrencyCode)
            .HasMaxLength(10);

        entity.Property(x => x.Pending)
            .IsRequired();

        entity.Property(x => x.IsDateUserEdited)
            .IsRequired();

        entity.Property(x => x.IsCategoryUserEdited)
            .IsRequired();

        entity.Property(x => x.Notes)
            .HasMaxLength(1000);

        entity.HasIndex(x => x.ArchivedAt);

        entity.Property(x => x.CreatedAt)
            .IsRequired();

        entity.Property(x => x.UpdatedAt)
            .IsRequired();

        entity.HasIndex(x => x.PlaidTransactionId)
            .IsUnique();

        entity.HasIndex(x => x.Date);

        entity.HasIndex(x => x.AccountId);

        entity.HasOne(x => x.Category)
            .WithMany(x => x.Transactions)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
