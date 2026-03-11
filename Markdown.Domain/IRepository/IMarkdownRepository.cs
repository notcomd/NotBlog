using Markdown.Domain.Entities;
using Markdown.Domain.SeedWork;
namespace Markdown.Domain.IRepository;

public interface IMarkdownRepository: IRepository<MarkDown>
{
    Task<string> MarkDownUploadAsync(Stream fileStream, string markdownName);
}