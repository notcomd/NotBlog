namespace Identity.Infrastructure.Configuration
{
    public class UserLoginEntityTypeConfiguration : IEntityTypeConfiguration<UserLoginHistory>
    {
        public void Configure(EntityTypeBuilder<UserLoginHistory> builder)
        {

            builder.ToTable("UserLoginHistory");

            builder.Ignore(en => en.DomainEvents);

            builder.HasKey(en => en.Id);

        }
    }
}
