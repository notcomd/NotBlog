namespace Markdown.Infrastructure.Idempotent;

public interface IRequestManagement
{
    Task<bool> ExecuteAsync(Guid id);

    Task CreateRequestForCommandAsync<T>(Guid id);
}