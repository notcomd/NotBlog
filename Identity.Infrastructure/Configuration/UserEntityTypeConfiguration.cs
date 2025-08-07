namespace Identity.Infrastructure.Configuration
{
    public class UserEntityTypeConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("User");

            builder.Ignore(b => b.DomainEventbus);

           
            builder.HasKey(x => x.Id);

            //builder.Property(x => x.UserGuid).HasColumnName("UserGuid").IsRequired();

            builder.Property(x => x.UserRoleGuid).HasColumnName("UserRoleGuid").IsRequired();

            builder.Property(x => x.UserName).HasColumnName("UserName").HasMaxLength(50);

            builder.Property(x => x.PasswordHash).HasColumnName("PasswordHash").IsRequired().HasMaxLength(100);

            builder.Property(x => x.ImageCover).HasColumnName("ImageCover").IsRequired(false);

            builder.Property(x => x.CreateDatetime).HasColumnName("CreateDateTime").IsRequired();

            // 其他属性配置

            builder.HasOne(on => on.PhoneNumber).WithOne()
                .HasForeignKey<PhoneNumber>(on => on.UserGuid);

            builder.HasOne(on => on.UserAccessFail).WithOne()
                .HasForeignKey<UserAccessFail>(on => on.UserGuid);

            builder.HasOne(on => on.UserSafety).WithOne()
                .HasForeignKey<UserSafety>(on => on.UserGuid);

            builder.HasMany(on => on.UserClaimsReadOnly).WithOne()
                .HasForeignKey(on => on.UserGuid);

            builder.HasIndex(en => new {  en.UserEmail })
                .HasDatabaseName("IX_User_UserGuid_UserEmail_UserPhone");

        }
    }
}