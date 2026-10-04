using Cardui.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cardui.Api.Data.Configurations;

public class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    /// <summary>
    /// Maps a category group and its unique key and name.
    /// </summary>
    public void Configure(EntityTypeBuilder<Group> entity)
    {
        entity.HasKey(x => x.Id);

        entity.Property(x => x.Key)
            .IsRequired()
            .HasMaxLength(100);

        entity.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        entity.Property(x => x.SortOrder)
            .IsRequired();

        entity.Property(x => x.CreatedAt)
            .IsRequired();

        entity.Property(x => x.UpdatedAt)
            .IsRequired();

        entity.HasIndex(x => x.Key)
            .IsUnique();

        entity.HasIndex(x => x.Name)
            .IsUnique();
    }
}
