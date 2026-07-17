using Message.Domain.Entities.Tweet;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Message.Infrastructure.EntityConfig;

public class TweetInteractionConfiguration : IEntityTypeConfiguration<TweetInteraction>
{
    public void Configure(EntityTypeBuilder<TweetInteraction> builder)
    {
        builder.ToTable("TweetInteractions");

        builder.HasKey(ti => ti.InteractGuid);

        builder.Property(ti => ti.InteractGuid)
            .IsRequired()
            .ValueGeneratedOnAdd();

        builder.Property(ti => ti.TweetGuid)
            .IsRequired();

        builder.Property(ti => ti.UserGuid)
            .IsRequired();

        builder.Property(ti => ti.Type)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(ti => ti.CreateTime)
            .IsRequired();

        builder.HasIndex(ti => new { ti.TweetGuid, ti.UserGuid, ti.Type }).IsUnique();
        builder.HasIndex(ti => ti.TweetGuid);
        builder.HasIndex(ti => ti.UserGuid);
    }
}
