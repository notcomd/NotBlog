namespace Markdown.Infrastructure.Configuration;

public class MarkHositoryEntityConfiguration: IEntityTypeConfiguration<MarkHistory>
{
    public void Configure(EntityTypeBuilder<MarkHistory> builder)
    {
        builder.Ignore(en => en.DomainEventbus);

        builder.ToTable("MarkHistory");
        
        builder.Property(x => x.Id).UseHiLo("MarkHositoryseq");
        
        
    }
}