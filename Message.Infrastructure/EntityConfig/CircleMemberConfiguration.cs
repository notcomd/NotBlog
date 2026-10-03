
namespace Message.Infrastructure.EntityConfig;

/// <summary>配置圈子成员实体 <c>CircleMember</c> 到 CircleMembers 表的映射。</summary>
public class CircleMemberConfiguration : IEntityTypeConfiguration<CircleMember>
{
    public void Configure(EntityTypeBuilder<CircleMember> builder)
    {
        builder.ToTable("CircleMembers");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .ValueGeneratedOnAdd();

        // 保留历史列名（原属性名拼写错误 CircleMembleGuid，已更正为 CircleMemberGuid）
        builder.Property(m => m.CircleMemberGuid)
            .HasColumnName("CircleMembleGuid");

        builder.Property(m => m.CircleGuid)
            .IsRequired();

        builder.Property(m => m.UserGuid)
            .IsRequired();

        builder.Property(m => m.Role)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(m => m.Nickname)
            .HasMaxLength(50);

        builder.Property(m => m.Status)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(m => m.JoinTime)
            .IsRequired();

        builder.HasIndex(m => new { m.CircleGuid, m.UserGuid })
            .IsUnique();
        builder.HasIndex(m => m.UserGuid);
    }
}
