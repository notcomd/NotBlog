namespace Markdown.Infrastructure.Configuration;

public class MarkHistoryEntityConfiguration: IEntityTypeConfiguration<MarkHistory>
{
    public void Configure(EntityTypeBuilder<MarkHistory> builder)
    {
        builder.Ignore(en => en.DomainEvents);

        builder.ToTable("MarkHistory");
        
        builder.Property(x => x.Id).UseHiLo("MarkHistoryseq");
        
        
    }
}