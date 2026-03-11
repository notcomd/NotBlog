using FileDev.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FileDev.Infrastructure.EntityConfig;

public class NotFileEntityConfiguration: IEntityTypeConfiguration<NotFile>
{
    public void Configure(EntityTypeBuilder<NotFile> builder)
    {
        builder.Ignore(en => en.DomainEventbus);
        builder.ToTable("NotFile");
        builder.Property(x => x.Id).UseHiLo("NotFileSeq");
        builder.HasKey(xn => xn.Id);
        
    }
}