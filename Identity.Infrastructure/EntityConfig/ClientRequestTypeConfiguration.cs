namespace Identity.Infrastructure.EntityConfig;

public class ClientRequestTypeConfiguration : IEntityTypeConfiguration<ClientRequest>
{
    public void Configure(EntityTypeBuilder<ClientRequest> builder)
    {
        builder.ToTable("ClientRequest");
    }
}