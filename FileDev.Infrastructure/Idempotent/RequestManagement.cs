using FileDev.Infrastructure.EntityFramework;

namespace FileDev.Infrastructure.Idempotent;

public class RequestManagement(NotFileDbContext notFileDbContext) : IRequestManagement
{
    private readonly NotFileDbContext _notFileDbContext =
        notFileDbContext ?? throw new ArgumentNullException(nameof(notFileDbContext));

    public async Task<bool> ExecuteAsync(Guid request)
    {
        var clientRequest = await _notFileDbContext.FindAsync<ClientRequest>(request);
        return clientRequest != null;
    }

    public async Task CreateRequestForCommandAsync<T>(Guid request)
    {
        var exists = await ExecuteAsync(request);
        var requset = exists
            ? throw new Exception("Request already exists")
            : new ClientRequest
            {
                ClientRequestId = request,
                ClientRequestName = typeof(T).Name,
                CreatedDate = DateTime.UtcNow
            };
        _notFileDbContext.Add(requset);
        await _notFileDbContext.SaveChangesAsync();
    }
}