using Cardui.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cardui.Api.Data.Configurations;

public class SubGroupConfiguration : IEntityTypeConfiguration<SubGroup>
{
    public void Configure(EntityTypeBuilder<SubGroup> entity)
    {
        entity.HasKey(x => x.Id);

        entity.Property(x => x.Key)
            .IsRequired()
            .HasMaxLength(100);

        entity.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        entity.Property(x => x.IsSystem)
            .IsRequired();

        entity.Property(x => x.SortOrder)
            .IsRequired();

        entity.Property(x => x.CreatedAt)
            .IsRequired();

        entity.Property(x => x.UpdatedAt)
            .IsRequired();

        entity.HasIndex(x => x.Key)
            .IsUnique();

        entity.HasIndex(x => x.HouseholdId);

        entity.HasOne<Household>()
            .WithMany()
            .HasForeignKey(x => x.HouseholdId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasIndex(x => new { x.GroupId, x.Name })
            .IsUnique();

        entity.HasOne(x => x.Group)
            .WithMany(x => x.SubGroups)
            .HasForeignKey(x => x.GroupId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
