using Markdown.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Markdown.Infrastructure.Configuration;

public class MarkDownEntityConfiguration : IEntityTypeConfiguration<MarkDown>
{
    public void Configure(EntityTypeBuilder<MarkDown> builder)
    {
        builder.ToTable("MarkDown");
        builder.Ignore(en => en.DomainEventbus);
        builder.Property(en => en.Id).UseHiLo("MarkDownseq");
    }
}