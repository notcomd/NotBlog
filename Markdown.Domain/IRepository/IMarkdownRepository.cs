namespace Markdown.Domain.IRepository;

public interface IMarkdownRepository
{
    Task<string> MarkDownUploadAsync(Stream fileStream, string markdownName);
}