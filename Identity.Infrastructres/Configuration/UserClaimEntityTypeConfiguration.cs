namespace Identity.Infrastructure.Configuration
{
    internal class UserClaimEntityTypeConfiguration : IEntityTypeConfiguration<UserClaim>
    {
        public void Configure(EntityTypeBuilder<UserClaim> builder)
        {


            builder.ToTable("UserClaims");

            builder.Ignore(x => x.DomainEvents);

            builder.HasKey(en => en.Id);

        }
    }
}
