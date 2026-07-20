using Cardui.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cardui.Api.Data.Configurations;

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> entity)
    {
        entity.HasKey(x => x.Id);

        entity.Property(x => x.PlaidAccountId)
            .IsRequired()
            .HasMaxLength(200);

        entity.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        entity.Property(x => x.OfficialName)
            .HasMaxLength(300);

        entity.Property(x => x.Type)
            .IsRequired()
            .HasMaxLength(100);

        entity.Property(x => x.Subtype)
            .HasMaxLength(100);

        entity.Property(x => x.Mask)
            .HasMaxLength(20);

        entity.Property(x => x.CurrentBalance)
            .HasPrecision(18, 2);

        entity.Property(x => x.AvailableBalance)
            .HasPrecision(18, 2);

        entity.Property(x => x.IsoCurrencyCode)
            .HasMaxLength(10);

        entity.Property(x => x.IsActive)
            .IsRequired();

        entity.Property(x => x.CreatedAt)
            .IsRequired();

        entity.Property(x => x.UpdatedAt)
            .IsRequired();

        entity.HasIndex(x => x.PlaidAccountId)
            .IsUnique();

        entity.HasMany(x => x.Transactions)
            .WithOne(x => x.Account)
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
