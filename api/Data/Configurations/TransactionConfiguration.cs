using Cardui.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cardui.Api.Data.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> entity)
    {
        entity.HasKey(x => x.Id);

        entity.Property(x => x.PlaidTransactionId)
            .IsRequired()
            .HasMaxLength(200);

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

        entity.Property(x => x.Notes)
            .HasMaxLength(1000);

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
