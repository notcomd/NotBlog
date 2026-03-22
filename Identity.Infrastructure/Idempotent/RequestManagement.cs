namespace Identity.Infrastructure.Idempotent;

public class RequestManagement(IdentityDbContext context) : IRequestManagement
{
    private readonly IdentityDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<bool> ExecuteAsync(Guid request)
    {
        var data = await _context.FindAsync<ClientRequest>(request);
        return data != null;
    }

    public async Task CreateRequestForCommandAsync<T>(Guid request)
    {
        var @bool = await ExecuteAsync(request);
        var request1 = @bool
            ? throw new Exception("Request already exists")
            : new ClientRequest
            {
                ClientRequestId = request,
                ClientRequestName = typeof(T).Name,
                CreatedDate = DateTimeOffset.Now
            };
        await _context.AddAsync(request1);
        await _context.SaveChangesAsync();
    }
}