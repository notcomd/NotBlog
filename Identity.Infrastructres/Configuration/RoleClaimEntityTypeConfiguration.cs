namespace Identity.Infrastructure.Configuration
{
    public class RoleClaimEntityTypeConfiguration : IEntityTypeConfiguration<RoleClaim>
    {
        public void Configure(EntityTypeBuilder<RoleClaim> builder)
        {

            builder.ToTable("RoleClaims");

            //builder.Property(o => o.Id).UseHiLo("RoleClaimseq");
            builder.HasKey(e => e.Id);
            //builder.HasKey(x=>x.RoleClaimGuid)

            builder.Ignore(x => x.DomainEvents);

        }
    }
}
