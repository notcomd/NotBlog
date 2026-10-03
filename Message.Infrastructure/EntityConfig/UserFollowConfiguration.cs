
namespace Message.Infrastructure.EntityConfig;

/// <summary>配置用户关注关系实体 <c>UserFollow</c> 到 UserFollows 表的映射。</summary>
public class UserFollowConfiguration : IEntityTypeConfiguration<UserFollow>
{
    public void Configure(EntityTypeBuilder<UserFollow> builder)
    {
        builder.ToTable("UserFollows");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Id)
            .ValueGeneratedOnAdd();

        builder.Property(f => f.FollowerGuid)
            .IsRequired();

        builder.Property(f => f.FolloweeGuid)
            .IsRequired();

        builder.Property(f => f.CreateTime)
            .IsRequired();

        builder.HasIndex(f => new { f.FollowerGuid, f.FolloweeGuid })
            .IsUnique();
        builder.HasIndex(f => f.FolloweeGuid);
    }
}
