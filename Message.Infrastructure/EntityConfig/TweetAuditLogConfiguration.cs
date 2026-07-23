namespace Message.Infrastructure.EntityConfig;

public class TweetAuditLogConfiguration : IEntityTypeConfiguration<TweetAuditLog>
{
    public void Configure(EntityTypeBuilder<TweetAuditLog> builder)
    {
        builder.ToTable("TweetAuditLogs");

        builder.HasKey(tal => tal.AuditGuid);

        builder.Property(tal => tal.AuditGuid)
            .IsRequired()
            .ValueGeneratedOnAdd();

        builder.Property(tal => tal.TweetGuid)
            .IsRequired();

        builder.Property(tal => tal.AuditorGuid)
            .IsRequired();

        builder.Property(tal => tal.Action)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(tal => tal.Reason)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(tal => tal.AuditTime)
            .IsRequired();

        builder.HasIndex(tal => new { tal.TweetGuid, tal.AuditTime }).IsDescending(false, true);
        builder.HasIndex(tal => new { tal.AuditorGuid, tal.AuditTime }).IsDescending(false, true);
    }
}
