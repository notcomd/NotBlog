namespace Identity.Infrastructure.Configuration
{
    public class PhoneNumberEntityTypeConfigration : IEntityTypeConfiguration<PhoneNumber>
    {
        public void Configure(EntityTypeBuilder<PhoneNumber> builder)
        {
            builder.HasKey(en => en.Id);
            builder.HasIndex(en => en.PhoneCode).IsUnique();
            builder.Ignore(en => en.DomainEvents);
        }
    }
}
