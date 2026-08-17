namespace Identity.Infrastructure.EntityConfig;

public class UserExternalLoginEntityTypeConfiguration : IEntityTypeConfiguration<UserExternalLogin>
{
    public void Configure(EntityTypeBuilder<UserExternalLogin> builder)
    {
        builder.ToTable("UserExternalLogins");
        builder.Ignore(b => b.DomainEvents);
        builder.Ignore(o=>o.Id);
        builder.HasKey(e => e.LoginId);

        // 托管 EF 值生成（Guid v7 在实体层生成，不依赖 DB 默认值）
        builder.Property(e => e.LoginId)
            .ValueGeneratedNever();

        builder.Property(e => e.UserId)
            .IsRequired();

        builder.Property(e => e.Provider)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(e => e.ProviderKey)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(e => e.ProviderUnionId)
            .HasMaxLength(450);

        builder.Property(e => e.ProviderDisplayName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.EncryptedAccessToken)
            .HasMaxLength(2000);

        builder.Property(e => e.EncryptedRefreshToken)
            .HasMaxLength(2000);

        builder.Property(e => e.TokenExpiresAt);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.LastUsedAt)
            .IsRequired();

        // 唯一索引: 同一提供商下的 ProviderKey 唯一
        builder.HasIndex(e => new { e.Provider, e.ProviderKey })
            .IsUnique();

        // 查询索引: 按用户 ID 查找
        builder.HasIndex(e => e.UserId);

        // 忽略基类的 Int Id（使用 LoginId 作为主键）
        builder.Ignore(e => e.Id);
        builder.Ignore(e => e.DomainEvents);
    }
}