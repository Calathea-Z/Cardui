using Cardui.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cardui.Api.Data.Configurations;

public class ConversationQuestionConfiguration : IEntityTypeConfiguration<ConversationQuestion>
{
    /// <summary>
    /// Maps a question to its conversation.
    /// AnsweredAt is null while the question is still open.
    /// Deleting the conversation deletes the questions.
    /// </summary>
    public void Configure(EntityTypeBuilder<ConversationQuestion> entity)
    {
        entity.HasKey(x => x.Id);

        entity.Property(x => x.ConversationId)
            .IsRequired();

        entity.Property(x => x.Text)
            .IsRequired()
            .HasMaxLength(ConversationQuestion.TextMaxLength);

        entity.Property(x => x.CreatedAt)
            .IsRequired();

        entity.HasIndex(x => new { x.ConversationId, x.AnsweredAt });

        entity.HasOne(x => x.Conversation)
            .WithMany(x => x.Questions)
            .HasForeignKey(x => x.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
