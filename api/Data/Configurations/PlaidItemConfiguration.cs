using Cardui.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cardui.Api.Data.Configurations;

public class PlaidItemConfiguration : IEntityTypeConfiguration<PlaidItem>
{
    public void Configure(EntityTypeBuilder<PlaidItem> entity)
    {
        entity.HasKey(x => x.Id);

        entity.Property(x => x.PlaidItemId)
            .IsRequired()
            .HasMaxLength(200);

        entity.Property(x => x.AccessToken)
            .IsRequired()
            .HasMaxLength(2000);

        entity.Property(x => x.InstitutionId)
            .HasMaxLength(200);

        entity.Property(x => x.InstitutionName)
            .HasMaxLength(200);

        entity.Property(x => x.CreatedAt)
            .IsRequired();

        entity.Property(x => x.UpdatedAt)
            .IsRequired();

        entity.Property(x => x.TransactionsCursor)
            .HasMaxLength(1000);

        entity.Property(x => x.LastTransactionsSyncedAt);

        entity.Property(x => x.LastSyncStartedAt);

        entity.Property(x => x.LastSyncCompletedAt);

        entity.Property(x => x.LastSyncFailedAt);

        entity.Property(x => x.LastSyncError)
            .HasMaxLength(1000);

        entity.HasIndex(x => x.PlaidItemId)
            .IsUnique();

        entity.HasIndex(x => x.HouseholdId);

        entity.HasOne<Household>()
            .WithMany()
            .HasForeignKey(x => x.HouseholdId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasMany(x => x.Accounts)
            .WithOne(x => x.PlaidItem)
            .HasForeignKey(x => x.PlaidItemId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
