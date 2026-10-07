using Cardui.Api.Domain.TransactionImport;
using Cardui.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cardui.Api.Data.Configurations;

public class TransactionImportConfiguration : IEntityTypeConfiguration<TransactionImport>
{
    /// <summary>
    /// Maps an import batch, its household and account, and the lookup for recent batches.
    /// </summary>
    public void Configure(EntityTypeBuilder<TransactionImport> entity)
    {
        entity.HasKey(x => x.Id);

        entity.Property(x => x.FileName)
            .IsRequired()
            .HasMaxLength(TransactionImportLimits.MaxFileNameLength);

        entity.Property(x => x.ImportedCount)
            .IsRequired();

        entity.Property(x => x.CreatedAt)
            .IsRequired();

        entity.HasIndex(x => new { x.HouseholdId, x.CreatedAt });

        entity.HasOne<Household>()
            .WithMany()
            .HasForeignKey(x => x.HouseholdId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(x => x.Account)
            .WithMany()
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
