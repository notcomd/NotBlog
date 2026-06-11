using Identity.Infrastructure.Idempotent;

namespace Identity.Infrastructure.Configuration;

public class ClientRequestTypeConfiguration : IEntityTypeConfiguration<ClientRequest>
{
    public void Configure(EntityTypeBuilder<ClientRequest> builder)
    {
        builder.ToTable("ClientRequest");
    }
}