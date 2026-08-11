using MessageEntity = Message.Domain.Entities.Chat.Message;

namespace Message.Infrastructure.EntityConfig;

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

        builder.Property(m => m.MessageType)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(m => m.Status)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(m => m.Content)
            .HasMaxLength(4000);

        builder.Property(m => m.MediaUri)
            .HasMaxLength(2048);

        builder.Property(m => m.ThumbnailUri)
            .HasMaxLength(2048);

        builder.Property(m => m.FileName)
            .HasMaxLength(500);

        builder.Property(m => m.MimeType)
            .HasMaxLength(100);

        builder.Property(m => m.Caption)
            .HasMaxLength(500);

        builder.Property(m => m.LocationName)
            .HasMaxLength(200);

        builder.Property(m => m.LinkUrl)
            .HasMaxLength(2048);

        builder.Property(m => m.LinkTitle)
            .HasMaxLength(200);

        builder.Property(m => m.LinkDescription)
            .HasMaxLength(500);

        builder.Property(m => m.ExpressionCode)
            .HasMaxLength(100);

        builder.Property(m => m.SentTime)
            .IsRequired();

        builder.Property(m => m.IsRecalled)
            .IsRequired();

        builder.Property(m => m.IsEncrypted)
            .IsRequired();

        builder.Property(m => m.IsForwarded)
            .IsRequired();

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