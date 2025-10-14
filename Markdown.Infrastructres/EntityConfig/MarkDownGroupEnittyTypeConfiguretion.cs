
using System.Runtime.Intrinsics.X86;
using Markdown.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Markdown.Infrastructres.EntityConfig;

public class MarkDownGroupEnittyTypeConfiguretion : IEntityTypeConfiguration<MarkDownGroup>
{
    public void Configure(EntityTypeBuilder<MarkDownGroup> builder)
    {
        builder.ToTable("MarkDownGroup");
        builder.HasKey(x => x.Id);
        builder.Ignore(x => x.DomainEvents);

        builder.Property(x => x.MarkGroupName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.MarkDownGroupDescription)
            .HasMaxLength(500);

    }
}
