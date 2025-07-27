
namespace Identity.Domain.Events
{
    public class RoleClaimEntityTypeConfiguration : IEntityTypeConfiguration<RoleClaim>
    {
        public void Configure(EntityTypeBuilder<RoleClaim> builder)
        {

            builder.ToTable("RoleClaims", "Identity");
            
            builder.Ignore(x => x.DomainEventbus);
                    
        }
    }
}
