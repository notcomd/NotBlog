using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Identity.Infrastructure.RequestManager;

namespace Identity.Infrastructure.Configuration
{
    public class ClientRequestEntityTypeConfiguration  : IEntityTypeConfiguration<ClientRequest>
    {
        public void Configure(EntityTypeBuilder<ClientRequest> builder)
        {
            builder.ToTable("ClientRequests");
        }
    }
}
