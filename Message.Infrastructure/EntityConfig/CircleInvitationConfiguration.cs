
namespace Message.Infrastructure.EntityConfig;

public class CircleInvitationConfiguration : IEntityTypeConfiguration<CircleInvitation>
{
    public void Configure(EntityTypeBuilder<CircleInvitation> builder)
    {
        builder.ToTable("CircleInvitations");

        builder.HasKey(i => i.InviteGuid);

        builder.Property(i => i.InviteGuid)
            .IsRequired()
            .ValueGeneratedOnAdd();

        builder.Property(i => i.CircleGuid)
            .IsRequired();

        builder.Property(i => i.InviterGuid)
            .IsRequired();

        builder.Property(i => i.InviteeGuid);

        builder.Property(i => i.Code)
            .HasMaxLength(6);

        builder.Property(i => i.Token);

        builder.Property(i => i.Type)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(i => i.Status)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(i => i.ExpireTime)
            .IsRequired();

        builder.Property(i => i.CreateTime)
            .IsRequired();

        builder.HasIndex(i => i.Code).IsUnique();
        builder.HasIndex(i => i.Token).IsUnique();
        builder.HasIndex(i => i.CircleGuid);
        builder.HasIndex(i => i.InviteeGuid);
    }
}
