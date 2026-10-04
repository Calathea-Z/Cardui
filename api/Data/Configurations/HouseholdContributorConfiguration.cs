using Cardui.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cardui.Api.Data.Configurations;

public class HouseholdContributorConfiguration : IEntityTypeConfiguration<HouseholdContributor>
{
    public void Configure(EntityTypeBuilder<HouseholdContributor> entity)
    {
        entity.HasKey(x => x.Id);

        entity.Property(x => x.HouseholdId)
            .IsRequired();

        entity.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(HouseholdContributor.NameMaxLength);

        entity.Property(x => x.IsVisible)
            .IsRequired()
            .HasDefaultValue(true);

        entity.Property(x => x.CreatedAt)
            .IsRequired();

        entity.Property(x => x.UpdatedAt)
            .IsRequired();

        entity.HasOne(x => x.Household)
            .WithMany(x => x.Contributors)
            .HasForeignKey(x => x.HouseholdId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasIndex(x => x.HouseholdId);
    }
}
