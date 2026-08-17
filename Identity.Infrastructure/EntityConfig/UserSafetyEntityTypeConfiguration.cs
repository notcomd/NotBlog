namespace Identity.Infrastructure.EntityConfig;

public class UserSafetyEntityTypeConfiguration : IEntityTypeConfiguration<UserSafety>
{
    public void Configure(EntityTypeBuilder<UserSafety> builder)
    {
        builder.ToTable("UserSafety");

        builder.Ignore(b => b.DomainEvents);
        // builder.Property(o => o.DomainEvents);

        builder.Ignore(o=>o.Id);

        builder.HasKey(x => x.UserSafetyGuid);
        builder.Property(x => x.UserGuid).HasColumnName("user_guid")
            .IsRequired();
    }
}