namespace Message.Infrastructure.EntityConfig;

/// <summary>配置动态通知实体 <c>TweetNotification</c> 到 TweetNotifications 表的映射。</summary>
public class TweetNotificationConfiguration : IEntityTypeConfiguration<TweetNotification>
{
    public void Configure(EntityTypeBuilder<TweetNotification> builder)
    {
        builder.ToTable("TweetNotifications");

        builder.HasKey(tn => tn.Id);

        builder.Property(tn => tn.Id)
            .IsRequired()
            .ValueGeneratedOnAdd();

        builder.Property(tn => tn.UserGuid)
            .IsRequired();

        builder.Property(tn => tn.Type)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(tn => tn.Title)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(tn => tn.Content)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(tn => tn.RefType)
            .HasMaxLength(20);

        builder.Property(tn => tn.RefGuid);

        builder.Property(tn => tn.IsRead)
            .IsRequired();

        builder.Property(tn => tn.CreateTime)
            .IsRequired();

        builder.HasIndex(tn => new { tn.UserGuid, tn.IsRead, tn.CreateTime }).IsDescending(false, false, true);
        builder.HasIndex(tn => tn.Type);
    }
}
