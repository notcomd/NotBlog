using Markdown.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Markdown.Infrastructure.Configuration;

public class MarkHositoryEntityConfiguration: IEntityTypeConfiguration<MarkHository>
{
    public void Configure(EntityTypeBuilder<MarkHository> builder)
    {
        builder.Ignore(en => en.DomainEventbus);

        builder.ToTable("MarkHository");
        
        builder.Property(x => x.Id).UseHiLo("MarkHositoryseq");
        
        
    }
}