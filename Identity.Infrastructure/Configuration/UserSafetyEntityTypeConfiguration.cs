namespace Identity.Infrastructure.Configuration
{
    public class UserSafetyEntityTypeConfiguration : IEntityTypeConfiguration<UserSafety>
    {
        public void Configure(EntityTypeBuilder<UserSafety> builder)
        {

            builder.ToTable("UserSafety");

            builder.Ignore(b => b.DomainEventbus).Ignore("IsActive").Ignore("IsLockOut").Ignore("IsDeleted");
           
            builder.HasKey(en => en.Id);             

        }
    }


}