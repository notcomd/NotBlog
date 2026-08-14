namespace Message.Infrastructure.EntityConfig;

public class ChatSessionConfiguration : IEntityTypeConfiguration<ChatSession>
{
    public void Configure(EntityTypeBuilder<ChatSession> builder)
    {
        builder.ToTable("ChatSessions");

        builder.HasKey(s => s.SessionId);

        builder.Property(s => s.SessionId)
            .IsRequired()
            .ValueGeneratedOnAdd();

        builder.Property(s => s.SessionType)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(s => s.SessionName)
            .HasMaxLength(200);

        builder.Property(s => s.CreatorId)
            .IsRequired();

        builder.Property(s => s.CreatedTime)
            .IsRequired();

        builder.Property(s => s.IsDismissed)
            .IsRequired();

        builder.Property(s => s.IsPinned)
            .IsRequired();

        builder.Property(s => s.IsMuted)
            .IsRequired();

        builder.Property(s => s.LastMessageContent)
            .HasMaxLength(500);

        // 修复（2026-08-15）：Participants 从 text+逗号分隔改为 PostgreSQL uuid[] 数组列。
        // 原因：仓储查询直接对 Participants 做 Contains/Count 过滤（如 GetByUserIdAsync、
        // GetPrivateSessionAsync），text 列无法翻译 HashSet.Contains → “could not be translated”。
        // Npgsql 对 HashSet<Guid> ↔ uuid[] 为原生映射，且能将 Contains 翻译为 @> 操作符、
        // Count 翻译为 cardinality()。
        builder.Property(s => s.Participants)
            .HasColumnName("Participants")
            .HasColumnType("uuid[]");
        builder.Ignore(s => s.UnreadCount);
        builder.Ignore(s => s.LastReadTime);

        builder.HasIndex(s => s.CreatorId);
        builder.HasIndex(s => s.GroupId)
            .IsUnique()
            .HasFilter("\"GroupId\" IS NOT NULL");
    }
}