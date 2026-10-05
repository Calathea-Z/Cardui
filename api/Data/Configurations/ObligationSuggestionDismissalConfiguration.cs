using Cardui.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cardui.Api.Data.Configurations;

public class ObligationSuggestionDismissalConfiguration
    : IEntityTypeConfiguration<ObligationSuggestionDismissal>
{
    /// <summary>
    /// Maps a dismissed recurring pattern to its household.
    /// One household dismisses a pattern key once. Deleting the household deletes the dismissal.
    /// </summary>
    public void Configure(EntityTypeBuilder<ObligationSuggestionDismissal> entity)
    {
        entity.HasKey(x => x.Id);

        entity.Property(x => x.HouseholdId)
            .IsRequired();

        entity.Property(x => x.Key)
            .IsRequired()
            .HasMaxLength(ObligationSuggestionDismissal.KeyMaxLength);

        entity.Property(x => x.DismissedAt)
            .IsRequired();

        entity.HasOne(x => x.Household)
            .WithMany()
            .HasForeignKey(x => x.HouseholdId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasIndex(x => new { x.HouseholdId, x.Key })
            .IsUnique();
    }
}
