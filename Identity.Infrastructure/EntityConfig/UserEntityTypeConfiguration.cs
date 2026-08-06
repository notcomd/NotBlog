namespace Identity.Infrastructure.EntityConfig;

public class UserEntityTypeConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("User");

        builder.Ignore(b => b.DomainEvents);

        builder.Property(o => o.Id).UseHiLo("Userseq");

        builder.OwnsOne(o => o.UserAddress);

        // builder.Property()

        builder.HasKey(x => x.UserGuid);

        builder.Property(x => x.UserGuid).HasColumnName("user_guid").IsRequired();

        // P5：UserEmail 唯一索引——并发注册同邮箱由 DB 约束终结（先查后插的 TOCTOU 由唯一索引兜底）
        builder.HasIndex(x => x.UserEmail).IsUnique();

        builder.Property(x => x.UserRoleGuid).HasColumnName("user_role_guid").IsRequired();

        builder.Property(x => x.UserName).HasColumnName("user_name").IsRequired().HasMaxLength(50);

        builder.Property(x => x.PasswordHash).HasColumnName("password_hash").IsRequired().HasMaxLength(100);

        builder.Property(x => x.ImageCover).HasColumnName("image_cover").IsRequired(false);

        builder.Property(x => x.CreateDatetime).HasColumnName("create_datetime").IsRequired();

        // 其他属性配置

        builder.OwnsOne(on => on.PhoneNumber);

        builder.HasOne(on => on.UserAccessFail).WithOne()
            .HasForeignKey<UserAccessFail>(on => on.UserGuid);

        builder.HasOne(on => on.UserSafety).WithOne()
            .HasForeignKey<UserSafety>(on => on.UserGuid);
    }
}