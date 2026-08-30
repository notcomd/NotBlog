using Message.Domain.Entities.Announcement;

namespace Message.Infrastructure.EntityConfig;

/// <summary>
/// 公报实体映射配置
/// </summary>
public class AnnouncementConfiguration : IEntityTypeConfiguration<Announcement>
{
    public void Configure(EntityTypeBuilder<Announcement> builder)
    {
        builder.ToTable("Announcements");
        builder.HasKey(a => a.AnnouncementGuid);

        builder.Property(a => a.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(a => a.Content)
            .HasMaxLength(5000)
            .IsRequired();

        builder.HasIndex(a => a.CreatedAt);
        builder.HasIndex(a => new { a.IsRecalled, a.CreatedAt });
    }
}