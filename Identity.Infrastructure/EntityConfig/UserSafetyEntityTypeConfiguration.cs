namespace Identity.Infrastructure.EntityConfig;

public class UserSafetyEntityTypeConfiguration : IEntityTypeConfiguration<UserSafety>
{
    public void Configure(EntityTypeBuilder<UserSafety> builder)
    {
        builder.ToTable("UserSafety");

        builder.Ignore(b => b.DomainEvents);
        // builder.Property(o => o.DomainEvents);

        builder.Ignore(o=>o.Id);

        builder.HasKey(x => x.UserSafetyGuid);
        builder.Property(x => x.UserGuid).HasColumnName("user_guid")
            .IsRequired();

        // 二次验证默认开启：历史数据迁移时默认同步为 true
        builder.Property(x => x.IsTwoFactorEnabled)
            .HasColumnName("is_two_factor_enabled")
            .IsRequired()
            .HasDefaultValue(true);
    }
}