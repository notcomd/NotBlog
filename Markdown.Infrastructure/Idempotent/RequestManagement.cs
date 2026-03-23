using Markdown.Infrastructure.EntityFramework;

namespace Markdown.Infrastructure.Idempotent;

public class RequestManagement(MarkDownDbContext context) : IRequestManagement
{
    private readonly MarkDownDbContext _context = context ?? throw new ArgumentNullException(nameof(context));


    public async Task<bool> ExecuteAsync(Guid id)
    {
        var data = await _context.FindAsync<ClientRequest>(id);
        return data != null;
    }

    public async Task CreateRequestForCommandAsync<T>(Guid id)
    {
        var exists = await ExecuteAsync(id);
        var requset = exists
            ? throw new Exception("Request already exists")
            : new ClientRequest
            {
                ClientRequestId = id,
                ClientRequestName = typeof(T).Name,
                Created = DateTime.UtcNow,
            };
        _context.Add(requset);
        await _context.SaveChangesAsync();
    }
}