using Message.Domain.Entities.Group;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Message.Infrastructure.EntityFramework.EntityConfig;

public class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    public void Configure(EntityTypeBuilder<Group> builder)
    {
        builder.ToTable("Groups");

        builder.HasKey(g => g.GroupId);

        builder.Property(g => g.GroupId)
            .IsRequired()
            .ValueGeneratedOnAdd();

        builder.Property(g => g.GroupName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(g => g.Description)
            .HasMaxLength(500);

        builder.Property(g => g.OwnerId)
            .IsRequired();

        builder.Property(g => g.MaxMembers)
            .IsRequired();

        builder.Property(g => g.IsPublic)
            .IsRequired();

        builder.Property(g => g.AllowMemberInvite)
            .IsRequired();

        builder.Property(g => g.AllowMemberEditInfo)
            .IsRequired();

        builder.Property(g => g.CreatedTime)
            .IsRequired();

        builder.Property(g => g.IsDismissed)
            .IsRequired();

        builder.Ignore(g => g.Members);
        builder.Ignore(g => g.MemberCount);

        builder.HasIndex(g => g.OwnerId);
        builder.HasIndex(g => g.IsPublic);
    }
}