namespace Message.Infrastructure.EntityConfig;

/// <summary>用户资料表配置（UserInfos，UserId 主键）。</summary>
public class UserInfoConfiguration : IEntityTypeConfiguration<UserInfo>
{
    public void Configure(EntityTypeBuilder<UserInfo> builder)
    {
        builder.ToTable("UserInfos");

        builder.HasKey(u => u.UserId);

        builder.Property(u => u.UserId)
            .IsRequired()
            .ValueGeneratedOnAdd();

        builder.Property(u => u.Level)
            .IsRequired();

        builder.Property(u => u.Coins)
            .IsRequired();

        builder.Property(u => u.Experience)
            .IsRequired();

        builder.Property(u => u.BackgroundCoverUrl)
            .HasMaxLength(2048);

        builder.Property(u => u.CreateTime)
            .IsRequired();

        builder.Property(u => u.UpdateTime)
            .IsRequired();

        // 邮箱查找索引（GET /api/users/lookup 按邮箱精确匹配；非唯一——Identity 才是邮箱唯一性真相源）
        builder.HasIndex(u => u.Email);
    }
}
