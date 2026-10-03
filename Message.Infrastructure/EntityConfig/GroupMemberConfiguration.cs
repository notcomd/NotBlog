namespace Message.Infrastructure.EntityConfig;

/// <summary>配置群成员实体 <c>GroupMember</c> 到 GroupMembers 表的映射。</summary>
public class GroupMemberConfiguration : IEntityTypeConfiguration<GroupMember>
{
    public void Configure(EntityTypeBuilder<GroupMember> builder)
    {
        builder.ToTable("GroupMembers");

        builder.HasKey(gm => gm.MemberId);

        builder.Property(gm => gm.MemberId)
            .IsRequired()
            .ValueGeneratedOnAdd();

        builder.Property(gm => gm.GroupId)
            .IsRequired();

        builder.Property(gm => gm.UserId)
            .IsRequired();

        builder.Property(gm => gm.Role)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(gm => gm.Nickname)
            .HasMaxLength(100);

        builder.Property(gm => gm.JoinTime)
            .IsRequired();

        builder.Property(gm => gm.IsMuted)
            .IsRequired();

        builder.Property(gm => gm.IsBanned)
            .IsRequired();

        builder.HasIndex(gm => new { gm.GroupId, gm.UserId })
            .IsUnique();
    }
}