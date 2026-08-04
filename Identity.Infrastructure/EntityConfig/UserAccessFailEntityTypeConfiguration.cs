namespace Identity.Infrastructure.EntityConfig;

public class UserAccessFailEntityTypeConfiguration : IEntityTypeConfiguration<UserAccessFail>
{
    public void Configure(EntityTypeBuilder<UserAccessFail> builder)
    {
        builder.ToTable("UserAccessFail");

        builder.Ignore(b => b.DomainEvents);

        builder.Property(o => o.Id).UseHiLo("UserAccessFailseq");
    }
}