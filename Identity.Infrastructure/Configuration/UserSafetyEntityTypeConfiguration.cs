namespace Identity.Infrastructure.Configuration
{
    public class UserSafetyEntityTypeConfiguration : IEntityTypeConfiguration<UserSafety>
    {
        public void Configure(EntityTypeBuilder<UserSafety> builder)
        {
            builder.ToTable("UserSafety");

            builder.Ignore(b => b.DomainEventbus);
            // builder.Property(o => o.DomainEventbus);        

            builder.Property(x => x.Id).UseHiLo("UserSafarseq");

            

        }
    }


}