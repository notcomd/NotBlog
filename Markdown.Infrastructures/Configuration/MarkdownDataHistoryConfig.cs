using Markdown.Infrastructures.EntityConfig;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Markdown.Infrastructures.Configuration;

public class MarkdownDataHistoryConfig: IDesignTimeDbContextFactory<MarkdownDataDbContext>

{
    public MarkdownDataDbContext CreateDbContext(string[] args)
    {
        var build = new DbContextOptionsBuilder<MarkdownDataDbContext>();
        build.UseNpgsql("");
        return new MarkdownDataDbContext(build.Options);
        //throw new NotImplementedException();
    }
}