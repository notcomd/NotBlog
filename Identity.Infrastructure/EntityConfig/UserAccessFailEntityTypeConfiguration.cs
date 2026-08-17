namespace Identity.Infrastructure.EntityConfig;

public class UserAccessFailEntityTypeConfiguration : IEntityTypeConfiguration<UserAccessFail>
{
    public void Configure(EntityTypeBuilder<UserAccessFail> builder)
    {
        builder.ToTable("UserAccessFail");
        builder.HasKey(x => x.UserAccessFailGuid);
        builder.Ignore(b => b.DomainEvents);
        builder.Ignore(o=>o.Id);

        //builder.Property(o => o.Id).UseHiLo("UserAccessFailseq");
    }
}