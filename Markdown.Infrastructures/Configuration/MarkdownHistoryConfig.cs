using Markdown.Domain.Entities;
using Markdown.Infrastructures.EntityConfig;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Markdown.Infrastructures.Configuration;

public class MarkdownHistoryConfig:IDesignTimeDbContextFactory<MarkdownDbContext>
{
    public MarkdownDbContext CreateDbContext(string[] args)
    {
        var build = new DbContextOptionsBuilder<MarkdownDbContext>();
        build.UseNpgsql("");
        //throw new NotImplementedException();
        return new MarkdownDbContext(build.Options);
    }
}