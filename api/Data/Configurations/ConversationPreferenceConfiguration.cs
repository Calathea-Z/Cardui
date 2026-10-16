using Cardui.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cardui.Api.Data.Configurations;

public class ConversationPreferenceConfiguration : IEntityTypeConfiguration<ConversationPreference>
{
    /// <summary>
    /// Maps a note to its conversation.
    /// The stored name is unique inside the conversation. The service treats a different casing as the same name.
    /// Deleting the conversation deletes the notes.
    /// </summary>
    public void Configure(EntityTypeBuilder<ConversationPreference> entity)
    {
        entity.HasKey(x => x.Id);

        entity.Property(x => x.ConversationId)
            .IsRequired();

        entity.Property(x => x.Key)
            .IsRequired()
            .HasMaxLength(ConversationPreference.KeyMaxLength);

        entity.Property(x => x.Value)
            .IsRequired()
            .HasMaxLength(ConversationPreference.ValueMaxLength);

        entity.Property(x => x.UpdatedAt)
            .IsRequired();

        entity.HasIndex(x => new { x.ConversationId, x.Key })
            .IsUnique();

        entity.HasOne(x => x.Conversation)
            .WithMany(x => x.Preferences)
            .HasForeignKey(x => x.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
