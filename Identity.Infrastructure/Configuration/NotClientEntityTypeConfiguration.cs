namespace Identity.Infrastructure.Configuration
{
    public class NotClientEntityTypeConfiguration : IEntityTypeConfiguration<NotClient>
    {
        public void Configure(EntityTypeBuilder<NotClient> builder)
        {

            builder.ToTable("NotClient");

            builder.Ignore(b => b.DomainEventbus);

            //builder.Property(x => x.Id).UseHiLo("NotClientseq");

            builder.HasKey(xn => xn.Id);

            
            builder.Property(en => en.NotClientType).HasConversion<string>().HasMaxLength(50);

        }
    }

}