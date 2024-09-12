using System.Data.Common;
using Markdown.Domain.Entities;
using Markdown.Infrastructures.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Markdown.Infrastructures.EntityConfig;

public class MarkdownDataDbContext:DbContext
{

   // private readonly OptionsManager<DbContextConfiguration> _optionsManager;
    
    public DbSet<MarkdownData> MarkdownDataModels { get; set; }

    public MarkdownDataDbContext(DbContextOptions<MarkdownDataDbContext> options) : base(options)
    {
        
    }
    
    private readonly OptionsManager<DbContextConfiguration> _optionsManager;

    public MarkdownDataDbContext(DbContextOptions<MarkdownDataDbContext> options,
        OptionsManager<DbContextConfiguration> optionsManager) : base(options)
    {
        _optionsManager = optionsManager;
    }


    
}