namespace Identity.Infrastructure.Idempotent;

public class RequestManagement(IdentityDbContext context) : IRequestManagement
{
    private readonly IdentityDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

    /// <summary>
    /// 执行请求
    /// </summary>
    /// <param name="request">请求 ID</param>
    /// <returns>是否成功执行</returns>
    public async Task<bool> ExecuteAsync(Guid request)
    {
        var data = await _context.FindAsync<ClientRequest>(request);
        return data != null;
    }

    /// <summary>
    /// 创建为命令创建请求
    /// </summary>
    /// <param name="request">请求 ID</param>
    /// <typeparam name="T">命令类型</typeparam>
    /// <returns>是否成功创建</returns>
    public async Task CreateRequestForCommandAsync<T>(Guid request)
    {
        var @bool = await ExecuteAsync(request);
        var request1 = @bool
            ? throw new Exception("Request already exists")
            : new ClientRequest
            {
                ClientRequestId = request,
                ClientRequestName = typeof(T).Name,
                CreatedDate = DateTimeOffset.UtcNow
            };
        await _context.AddAsync(request1);
        await _context.SaveChangesAsync();
    }
}