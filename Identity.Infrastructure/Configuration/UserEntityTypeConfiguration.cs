namespace Identity.Infrastructure.Configuration
{
    public class UserEntityTypeConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("User");

            builder.Ignore(b => b.DomainEventbus);

            builder.Property(o => o.Id).UseHiLo("Userseq");

            builder.OwnsOne(o => o.UserAddress);

            // builder.Property()

            builder.HasKey(x => x.UserGuid);

            builder.Property(x => x.UserGuid).HasColumnName("user_guid").IsRequired();

            builder.Property(x => x.UserRoleGuid).HasColumnName("user_role_guid").IsRequired();

            builder.Property(x => x.UserName).HasColumnName("user_name").IsRequired().HasMaxLength(50);

            builder.Property(x => x.PasswordHash).HasColumnName("password_hash").IsRequired().HasMaxLength(100);

            builder.Property(x => x.ImageCover).HasColumnName("image_cover").IsRequired(false);

            builder.Property(x => x.CreateDatetime).HasColumnName("create_datetime").IsRequired();

            // 其他属性配置

            builder.HasOne(on => on.PhoneNumber).WithOne()
                .HasForeignKey<PhoneNumber>(on => on.UserGuid);

            builder.HasOne(on => on.UserAccessFail).WithOne()
                .HasForeignKey<UserAccessFail>(on => on.UserGuid);

            builder.HasOne(on => on.UserSafety).WithOne()
                .HasForeignKey<UserSafety>(on => on.UserGuid);

        }
    }
}