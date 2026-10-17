using Cardui.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cardui.Api.Data.Configurations;

public class ConversationPlanLinkConfiguration : IEntityTypeConfiguration<ConversationPlanLink>
{
    /// <summary>
    /// Maps a conversation to a saved plan version in the same household.
    /// Deleting the conversation or the plan version deletes the link. The plan version itself is unchanged by the link.
    /// </summary>
    public void Configure(EntityTypeBuilder<ConversationPlanLink> entity)
    {
        entity.HasKey(x => new { x.ConversationId, x.PlanVersionId });

        entity.Property(x => x.LinkedAt)
            .IsRequired();

        entity.HasIndex(x => x.PlanVersionId);

        entity.HasOne(x => x.Conversation)
            .WithMany(x => x.PlanLinks)
            .HasForeignKey(x => x.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(x => x.PlanVersion)
            .WithMany()
            .HasForeignKey(x => x.PlanVersionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
