namespace Message.Infrastructure.EntityConfig;

public class TweetInteractionConfiguration : IEntityTypeConfiguration<TweetInteraction>
{
    public void Configure(EntityTypeBuilder<TweetInteraction> builder)
    {
        builder.ToTable("TweetInteractions");

        builder.HasKey(ti => ti.Id);

        builder.Property(ti => ti.Id)
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
