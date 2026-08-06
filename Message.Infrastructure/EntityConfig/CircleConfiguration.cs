
namespace Message.Infrastructure.EntityConfig;

public class CircleConfiguration : IEntityTypeConfiguration<Circle>
{
    public void Configure(EntityTypeBuilder<Circle> builder)
    {
        builder.ToTable("Circles");

        builder.HasKey(c => c.CircleGuid);

        builder.Property(c => c.CircleGuid)
            .IsRequired()
            .ValueGeneratedOnAdd();

        builder.Property(c => c.OwnerGuid)
            .IsRequired();

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.Description)
            .HasMaxLength(500);

        builder.Property(c => c.AvatarUrl)
            .HasMaxLength(500);

        builder.Property(c => c.MaxMembers)
            .IsRequired();

        builder.Property(c => c.MemberCount)
            .IsRequired();

        builder.Property(c => c.Status)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(c => c.CreateTime)
            .IsRequired();

        builder.Property(c => c.DissolvedTime);

        builder.HasMany(c => c.Members)
            .WithOne()
            .HasForeignKey(m => m.CircleGuid)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.OwnerGuid);
        builder.HasIndex(c => c.CreateTime).IsDescending();
        builder.HasIndex(c => c.Status);
    }
}
