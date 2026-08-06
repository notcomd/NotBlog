
namespace Message.Infrastructure.EntityConfig;

public class CircleMemberConfiguration : IEntityTypeConfiguration<CircleMember>
{
    public void Configure(EntityTypeBuilder<CircleMember> builder)
    {
        builder.ToTable("CircleMembers");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .ValueGeneratedOnAdd();

        builder.Property(m => m.CircleGuid)
            .IsRequired();

        builder.Property(m => m.UserGuid)
            .IsRequired();

        builder.Property(m => m.Role)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(m => m.Nickname)
            .HasMaxLength(50);

        builder.Property(m => m.Status)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(m => m.JoinTime)
            .IsRequired();

        builder.HasIndex(m => new { m.CircleGuid, m.UserGuid })
            .IsUnique();
        builder.HasIndex(m => m.UserGuid);
    }
}
