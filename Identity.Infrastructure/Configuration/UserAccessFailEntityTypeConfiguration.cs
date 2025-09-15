namespace Identity.Infrastructure.Configuration
{
    public class UserAccessFailEntityTypeConfiguration : IEntityTypeConfiguration<UserAccessFail>
    {
        public void Configure(EntityTypeBuilder<UserAccessFail> builder)
        {

            builder.ToTable("UserAccessFail");

            builder.Ignore(b => b.DomainEvents).Ignore("IsLockOut");

            builder.HasKey(b => b.Id);


        }
    }
}