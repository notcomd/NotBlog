namespace Identity.Infrastructure.Configuration
{
    public class Author2EntityTypeConfiguration : IEntityTypeConfiguration<Author2>
    {
        public void Configure(EntityTypeBuilder<Author2> builder)
        {

            builder.ToTable("Author2");

            builder.Ignore(b => b.DomainEventbus);

            builder.Property(o => o.Id).UseHiLo("Author2seq");

        }
    }
}