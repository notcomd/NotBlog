using Identity.Infrastructure.RequestManager;

namespace Identity.Infrastructure.Configuration
{
    public class ClientRequestEntityTypeConfiguration : IEntityTypeConfiguration<ClientRequest>
    {
        public void Configure(EntityTypeBuilder<ClientRequest> builder)
        {
            builder.ToTable("ClientRequests");
            builder.HasKey(en => en.Id);
        }
    }
}
