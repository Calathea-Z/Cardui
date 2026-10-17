using Cardui.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cardui.Api.Data.Configurations;

public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    /// <summary>
    /// Maps a conversation to its household.
    /// Status is stored as its member name. Deleting the household deletes the conversation.
    /// One household has at most one open conversation. Many closed conversations are allowed.
    /// </summary>
    public void Configure(EntityTypeBuilder<Conversation> entity)
    {
        entity.HasKey(x => x.Id);

        entity.Property(x => x.HouseholdId)
            .IsRequired();

        entity.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        entity.Property(x => x.CreatedAt)
            .IsRequired();

        entity.Property(x => x.UpdatedAt)
            .IsRequired();

        entity.Property(x => x.Summary)
            .HasMaxLength(Conversation.SummaryMaxLength);

        entity.HasIndex(x => x.HouseholdId);

        entity.HasIndex(x => x.HouseholdId, Conversation.OpenHouseholdIndexName)
            .IsUnique()
            .HasFilter("\"Status\" = 'Open'");

        entity.HasOne(x => x.Household)
            .WithMany(x => x.Conversations)
            .HasForeignKey(x => x.HouseholdId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
