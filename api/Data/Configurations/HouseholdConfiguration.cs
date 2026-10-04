using Cardui.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cardui.Api.Data.Configurations;

public class HouseholdConfiguration : IEntityTypeConfiguration<Household>
{
    /// <summary>
    /// Maps a household and the unique Clerk owner id.
    /// </summary>
    public void Configure(EntityTypeBuilder<Household> entity)
    {
        entity.HasKey(x => x.Id);

        entity.Property(x => x.OwnerClerkUserId)
            .IsRequired()
            .HasMaxLength(Household.OwnerClerkUserIdMaxLength);

        entity.Property(x => x.DisplayName)
            .IsRequired()
            .HasMaxLength(Household.DisplayNameMaxLength);

        entity.Property(x => x.CreatedAt)
            .IsRequired();

        entity.Property(x => x.UpdatedAt)
            .IsRequired();

        entity.HasIndex(x => x.OwnerClerkUserId)
            .IsUnique();
    }
}
