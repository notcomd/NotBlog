using FileDev.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FileDev.Infrastructure.EntityConfig;

public class NotFileGroupEntityConfiguration: IEntityTypeConfiguration<NotFileGroup>
{
    
    public void Configure(EntityTypeBuilder<NotFileGroup> builder)
    {
        builder.Ignore(en => en.DomainEventbus);
        builder.ToTable("NotFileGroup");
        builder.Property(x => x.Id).UseHiLo("NotFileGroupseq");
        builder.HasKey(x => x.Id);
    }
    
}