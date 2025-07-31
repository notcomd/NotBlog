
namespace Identity.Domain.Events
{
    public class RoleClaimEntityTypeConfiguration : IEntityTypeConfiguration<RoleClaim>
    {
        public void Configure(EntityTypeBuilder<RoleClaim> builder)
        {

            builder.ToTable("RoleClaims", "Identity");
            
            builder.Property(o => o.Id).UseHiLo("RoleClaimseq");

            builder.Ignore(x => x.DomainEventbus);
                    
        }
    }
}
