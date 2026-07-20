using Cardui.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cardui.Api.Data.Configurations;

public class AccountBalanceSnapshotConfiguration
    : IEntityTypeConfiguration<AccountBalanceSnapshot>
{
    public void Configure(EntityTypeBuilder<AccountBalanceSnapshot> entity)
    {
        entity.HasKey(x => x.Id);

        entity.Property(x => x.Date)
            .IsRequired();

        entity.Property(x => x.CurrentBalance)
            .HasPrecision(18, 2);

        entity.Property(x => x.AvailableBalance)
            .HasPrecision(18, 2);

        entity.Property(x => x.IsoCurrencyCode)
            .HasMaxLength(10);

        entity.Property(x => x.CreatedAt)
            .IsRequired();

        entity.HasIndex(x => new { x.AccountId, x.Date })
            .IsUnique();

        entity.HasOne(x => x.Account)
            .WithMany(x => x.BalanceSnapshots)
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
