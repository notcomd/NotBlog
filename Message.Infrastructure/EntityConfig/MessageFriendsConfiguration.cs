namespace Message.Infrastructure.EntityConfig;

/// <summary>配置好友关系实体 <c>MessageFriends</c> 到 MessageFriends 表的映射。</summary>
public class MessageFriendsConfiguration : IEntityTypeConfiguration<MessageFriends>
{
    public void Configure(EntityTypeBuilder<MessageFriends> builder)
    {
        builder.ToTable("MessageFriends");

        builder.HasKey(f => f.FriendshipId);

        builder.Property(f => f.FriendshipId)
            .IsRequired()
            .ValueGeneratedOnAdd();

        builder.Property(f => f.UserId)
            .IsRequired();

        builder.Property(f => f.FriendId)
            .IsRequired();

        builder.Property(f => f.Status)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(f => f.Remark)
            .HasMaxLength(100);

        builder.Property(f => f.FriendGroupName)
            .HasMaxLength(50);

        builder.Property(f => f.CreatedTime)
            .IsRequired();

        // IsBlocked 已收敛为由 Status 派生的只读视图，EF 不再映射该列（保留既有表结构，避免破坏性迁移）
        builder.Ignore(f => f.IsBlocked);

        builder.Property(f => f.IsMuted)
            .IsRequired();

        builder.Property(f => f.IsStarred)
            .IsRequired();

        builder.HasIndex(f => new { f.UserId, f.FriendId })
            .IsUnique();

        builder.HasIndex(f => f.UserId);
        builder.HasIndex(f => f.FriendId);
    }
}