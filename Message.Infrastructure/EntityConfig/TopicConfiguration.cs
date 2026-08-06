
namespace Message.Infrastructure.EntityConfig;

public class TopicConfiguration : IEntityTypeConfiguration<Topic>
{
    public void Configure(EntityTypeBuilder<Topic> builder)
    {
        builder.ToTable("Topics");

        builder.HasKey(t => t.TopicGuid);

        builder.Property(t => t.TopicGuid)
            .IsRequired()
            .ValueGeneratedOnAdd();

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(t => t.Description)
            .HasMaxLength(200);

        builder.Property(t => t.CreatorGuid)
            .IsRequired();

        builder.Property(t => t.PostCount)
            .IsRequired();

        builder.Property(t => t.IsActive)
            .IsRequired();

        builder.Property(t => t.CreateTime)
            .IsRequired();

        builder.HasIndex(t => t.Name).IsUnique();
        builder.HasIndex(t => t.PostCount).IsDescending();
    }
}
