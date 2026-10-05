using Cardui.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cardui.Api.Data.Configurations;

public class IncomeRaiseConfiguration : IEntityTypeConfiguration<IncomeRaise>
{
    /// <summary>
    /// Maps an expected raise to its income source.
    /// One source has at most one raise on a date. Deleting the source deletes its raises.
    /// </summary>
    public void Configure(EntityTypeBuilder<IncomeRaise> entity)
    {
        entity.HasKey(x => x.Id);

        entity.Property(x => x.IncomeSourceId)
            .IsRequired();

        entity.Property(x => x.EffectiveDate)
            .IsRequired();

        entity.Property(x => x.TakeHomeAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        entity.HasOne(x => x.IncomeSource)
            .WithMany(x => x.Raises)
            .HasForeignKey(x => x.IncomeSourceId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasIndex(x => new { x.IncomeSourceId, x.EffectiveDate })
            .IsUnique();
    }
}
