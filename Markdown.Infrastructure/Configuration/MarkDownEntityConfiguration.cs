using Markdown.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Markdown.Infrastructure.Configuration;

public class MarkDownEntityConfiguration : IEntityTypeConfiguration<MarkDown>
{
    public void Configure(EntityTypeBuilder<MarkDown> builder)
    {
        builder.Ignore(en => en.DomainEventbus);
        builder.ToTable("NotFileGroup");
        builder.Property(x => x.Id).UseHiLo("NotFileGroupGuid");
        builder.HasKey(x => x.Id);

        builder.HasMany(en => en.MarkReviews)
            .WithOne(en => en.MarkDown)
            .HasForeignKey(en => en.MarkDownGuid);

        builder.HasMany(en => en.OldMarkDowns)
            .WithOne(en => en.MarkDown)
            .HasForeignKey(en => en.MarkDownGuid);
    }
}