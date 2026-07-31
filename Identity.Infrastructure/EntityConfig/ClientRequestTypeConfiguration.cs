using Identity.Infrastructure.Idempotent;

namespace Identity.Infrastructure.EntityConfig;

public class ClientRequestTypeConfiguration : IEntityTypeConfiguration<ClientRequest>
{
    public void Configure(EntityTypeBuilder<ClientRequest> builder)
    {
        builder.ToTable("ClientRequest");
        builder.HasKey(en=>en.ClientRequestId);
    }
}