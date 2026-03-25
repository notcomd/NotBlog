using Message.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

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

        builder.Ignore(s => s.Participants);
        builder.Ignore(s => s.UnreadCount);
        builder.Ignore(s => s.LastReadTime);

        builder.HasIndex(s => s.CreatorId);
        builder.HasIndex(s => s.GroupId)
            .IsUnique()
            .HasFilter("[GroupId] IS NOT NULL");
    }
}