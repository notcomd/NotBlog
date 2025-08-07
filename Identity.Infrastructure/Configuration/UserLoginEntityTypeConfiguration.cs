using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RabbitMQ.Client;

namespace Identity.Infrastructure.Configuration
{
    public class UserLoginEntityTypeConfiguration : IEntityTypeConfiguration<UserLoginHistory>
    {
        public void Configure(EntityTypeBuilder<UserLoginHistory> builder)
        {

            builder.ToTable("UserLoginHistory");

            builder.Ignore(en=>en.DomainEventbus);

            builder.HasKey(en => en.Id);
           
        }
    }
}
