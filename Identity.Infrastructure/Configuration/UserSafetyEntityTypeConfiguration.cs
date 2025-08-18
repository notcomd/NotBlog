namespace Identity.Infrastructure.Configuration
{
    public class UserSafetyEntityTypeConfiguration : IEntityTypeConfiguration<UserSafety>
    {
        public void Configure(EntityTypeBuilder<UserSafety> builder)
        {
            builder.ToTable("UserSafety");

            builder.Ignore(b => b.DomainEventbus);
           
            builder.HasKey(en => en.Id);

            builder.Property("IsActive");

            builder.Property("IsLockedOut");

            builder.Property("IsDeleted");

        }
    }


}