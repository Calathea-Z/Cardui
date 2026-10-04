using Cardui.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cardui.Api.Data.Configurations;

public class SubGroupConfiguration : IEntityTypeConfiguration<SubGroup>
{
    /// <summary>
    /// Maps a sub-group, its group, and the household that owns a custom row.
    /// </summary>
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
            .IsUnique()
            .HasFilter("\"IsSystem\"")
            .HasDatabaseName("IX_SubGroups_System_Key");

        entity.HasIndex(x => new { x.HouseholdId, x.Key })
            .IsUnique()
            .HasFilter("NOT \"IsSystem\"")
            .HasDatabaseName("IX_SubGroups_Household_Key");

        entity.HasIndex(x => new { x.GroupId, x.Name })
            .IsUnique()
            .HasFilter("\"IsSystem\"")
            .HasDatabaseName("IX_SubGroups_System_Group_Name");

        entity.HasIndex(x => new { x.HouseholdId, x.GroupId, x.Name })
            .IsUnique()
            .HasFilter("NOT \"IsSystem\"")
            .HasDatabaseName("IX_SubGroups_Household_Group_Name");

        entity.HasIndex(x => x.HouseholdId);

        entity.HasOne<Household>()
            .WithMany()
            .HasForeignKey(x => x.HouseholdId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(x => x.Group)
            .WithMany(x => x.SubGroups)
            .HasForeignKey(x => x.GroupId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
