using MessageEntity = Message.Domain.Entities.Chat.Message;

namespace Message.Infrastructure.EntityConfig;

/// <summary>配置消息实体 <c>Message</c> 到 Messages 表的映射。</summary>
public class MessageConfiguration : IEntityTypeConfiguration<MessageEntity>
{
    public void Configure(EntityTypeBuilder<MessageEntity> builder)
    {
        builder.ToTable("Messages");

        builder.HasKey(m => m.MessageId);

        builder.Property(m => m.MessageId)
            .IsRequired()
            .ValueGeneratedOnAdd();

        builder.Property(m => m.SessionId)
            .IsRequired();

        builder.Property(m => m.SenderId)
            .IsRequired();

        builder.Property(m => m.ReceiverId);

        builder.Property(m => m.Status)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(m => m.SentTime)
            .IsRequired();

        builder.Property(m => m.IsRecalled)
            .IsRequired();

        builder.Property(m => m.IsEncrypted)
            .IsRequired();

        builder.Property(m => m.IsForwarded)
            .IsRequired();

        // 消息内容已收敛为多态值对象 MessageContent，且消息本体落 MongoDB；
        // EF 此处不再映射内容相关列（避免破坏既有表结构），仅跟踪标识/状态元数据。
        builder.Ignore(m => m.Content);
        builder.Ignore(m => m.MessageType);

        builder.HasIndex(m => m.SessionId);
        builder.HasIndex(m => m.SenderId);
        builder.HasIndex(m => m.ReceiverId);
        builder.HasIndex(m => m.SentTime);

        builder.HasMany(m => m.Attachments)
            .WithOne()
            .HasForeignKey(a => a.MessageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}