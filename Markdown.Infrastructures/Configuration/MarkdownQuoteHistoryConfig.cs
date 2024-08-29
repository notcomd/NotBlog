using Markdown.Infrastructures.EntityConfig;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Markdown.Infrastructures.Configuration;

public class MarkdownQuoteHistoryConfig: IDesignTimeDbContextFactory<MarkdownQuoteDbContext>

{
    public MarkdownQuoteDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<MarkdownQuoteDbContext>();
        options.UseNpgsql("");
        return new MarkdownQuoteDbContext(options.Options);
    }
}