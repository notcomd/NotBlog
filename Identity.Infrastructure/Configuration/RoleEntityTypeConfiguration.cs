namespace Identity.Infrastructure.Configuration
{
    public class RoleEntityTypeConfiguration : IEntityTypeConfiguration<Roles>
    {
        public void Configure(EntityTypeBuilder<Roles> builder)
        {

            builder.ToTable("UserPrivacy");

            builder.Property(o => o.Id).UseHiLo("Roleseq");

            builder.Ignore(o => o.DomainEventbus);

            builder.Property(en => en.RoleAuthority).HasConversion<string>().HasMaxLength(50);

            builder.Property(en => en.RoleStatus).HasConversion<string>().HasMaxLength(50);


        }
    }
}