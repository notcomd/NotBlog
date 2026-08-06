using System.Text.Json;
using Npgsql;

namespace Markdown.Infrastructure.Idempotent;

/// <summary>
///     命令幂等执行实现：
///     1) 原子占位：直接插入 ClientRequest 记录，依赖 ClientRequestId 主键唯一约束做并发去重，
///        并发重复请求（相同 IdempotencyKey）只有一条能插入成功，其余捕获唯一键冲突后视为输家，
///        避免了"查→插"两步之间的 TOCTOU 竞态窗口；
///     2) 赢家执行命令后将响应 JSON 写入占位记录；
///     3) 输家轮询读取赢家写入的响应并返回，保证重试/并发请求拿到与首次执行一致的结果。
/// </summary>
public class RequestManagement(MarkDownDbContext context, ILogger<RequestManagement> logger)
    : IRequestManagement
{
    private readonly MarkDownDbContext _context = context ?? throw new ArgumentNullException(nameof(context));
    private readonly ILogger<RequestManagement> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>输家轮询等待赢家响应的总时长上限（10 次 × 100ms）</summary>
    private const int PollAttempts = 10;
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(100);

    public async Task<T> ExecuteIdempotentAsync<T>(Guid id, Func<Task<T>> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        var inserted = await CreateRequestForCommandAsync<T>(id);
        if (inserted)
        {
            // 赢家：执行命令并写入响应（业务异常由调用方处理，占位记录随事务回滚）
            var response = await operation();
            await UpdateResponseAsync(id, response);
            return response;
        }

        // 输家：轮询等待赢家写入响应后返回首次执行结果
        for (var attempt = 0; attempt < PollAttempts; attempt++)
        {
            var (handled, existing) = await ExecuteAsync<T>(id);
            if (handled && existing is not null && !EqualityComparer<T>.Default.Equals(existing, default))
            {
                _logger.LogInformation("幂等命中：请求 {IdempotencyKey} 已处理过，返回首次执行结果", id);
                return existing;
            }
            await Task.Delay(PollInterval);
        }

        throw new InvalidOperationException($"幂等请求 {id} 正在并发处理中，请稍后重试");
    }

    public async Task<(bool Handled, T? Response)> ExecuteAsync<T>(Guid id)
    {
        var data = await _context.FindAsync<ClientRequest>(id);
        if (data is null)
            return (false, default);

        if (string.IsNullOrWhiteSpace(data.ResponseJson))
        {
            // 占位中（赢家尚未写完响应）或历史遗留记录（旧版本未存响应）
            _logger.LogDebug("幂等记录 {IdempotencyKey} 尚无响应（占位中或历史数据）", id);
            return (true, default);
        }

        try
        {
            var response = JsonSerializer.Deserialize<T>(data.ResponseJson);
            return (true, response);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "幂等记录 {IdempotencyKey} 响应反序列化失败：{ResponseJson}", id, data.ResponseJson);
            throw new InvalidOperationException($"幂等记录 {id} 响应数据损坏", ex);
        }
    }

    public async Task<bool> CreateRequestForCommandAsync<T>(Guid id)
    {
        _context.Add(new ClientRequest
        {
            ClientRequestId = id,
            ClientRequestName = typeof(T).Name,
            Created = DateTimeOffset.UtcNow,
        });
        try
        {
            await _context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // 幂等占位冲突：同一 IdempotencyKey 已被并发请求占用
            _logger.LogInformation("幂等占位冲突：请求 {IdempotencyKey} 已被并发请求占用", id);
            return false;
        }
    }

    public async Task UpdateResponseAsync<T>(Guid id, T? response)
    {
        var data = await _context.FindAsync<ClientRequest>(id);
        if (data is null)
        {
            // 占位记录缺失（理论不可达：占位成功后才允许写响应），防御性处理
            _logger.LogWarning("幂等记录 {IdempotencyKey} 不存在，跳过响应写入", id);
            return;
        }

        data.ResponseJson = JsonSerializer.Serialize(response);
        await _context.SaveChangesAsync();
    }

    private static bool IsUniqueViolation(DbUpdateException ex)
    {
        return ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
    }
}
