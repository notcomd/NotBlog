using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Identity.Infrastructure.Configuration
{
    internal class UserClaimEntityTypeConfiguration : IEntityTypeConfiguration<UserClaim>
    {
        public void Configure(EntityTypeBuilder<UserClaim> builder)
        {


            builder.ToTable("UserClaims", "Identity");

            builder.Ignore(x => x.DomainEventbus);

           

        }
    }
}
