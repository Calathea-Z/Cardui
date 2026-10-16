using Cardui.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cardui.Api.Data.Configurations;

public class ConversationMessageConfiguration : IEntityTypeConfiguration<ConversationMessage>
{
    /// <summary>
    /// Maps one turn to its conversation.
    /// Role is stored as its member name. Sequence is unique inside the conversation.
    /// Deleting the conversation deletes the turns.
    /// </summary>
    public void Configure(EntityTypeBuilder<ConversationMessage> entity)
    {
        entity.HasKey(x => x.Id);

        entity.Property(x => x.ConversationId)
            .IsRequired();

        entity.Property(x => x.Sequence)
            .IsRequired();

        entity.Property(x => x.Role)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        entity.Property(x => x.Text)
            .IsRequired()
            .HasMaxLength(ConversationMessage.TextMaxLength);

        entity.Property(x => x.CreatedAt)
            .IsRequired();

        entity.HasIndex(x => new { x.ConversationId, x.Sequence })
            .IsUnique();

        entity.HasOne(x => x.Conversation)
            .WithMany(x => x.Messages)
            .HasForeignKey(x => x.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
