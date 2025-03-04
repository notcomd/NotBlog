using Markdown.Infrastructures.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Markdown.Infrastructures.Configuration;

public class MarkdownHistoryConfig : IDesignTimeDbContextFactory<MarkDownDbContext>
{
    public MarkDownDbContext CreateDbContext(string[] args)
    {
        var build = new DbContextOptionsBuilder<MarkDownDbContext>();
        build.UseNpgsql("");
        //throw new NotImplementedException();
        return new MarkDownDbContext(build.Options);
    }
}