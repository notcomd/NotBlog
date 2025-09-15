namespace Identity.Infrastructure.Configuration
{
    public class UserSafetyEntityTypeConfiguration : IEntityTypeConfiguration<UserSafety>
    {
        public void Configure(EntityTypeBuilder<UserSafety> builder)
        {

            builder.ToTable("UserSafety");

            builder.Ignore(b => b.DomainEvents).Ignore("IsActive").Ignore("IsLockOut").Ignore("IsDeleted");

            builder.HasKey(en => en.Id);

            builder.Property("PasswordSalt").HasColumnType("varchar").HasMaxLength(256);

            builder.Property("SecurityStamp").HasColumnType("varchar").HasMaxLength(256);

        }
    }


}