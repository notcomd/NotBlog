using System.Text.Json;
using CacheMemory.Core;
using NotMediator;

namespace Video.Web.API.Application.Behaviors;

/// <summary>
/// 幂等性管道行为 — 通过 Redis 检查请求 ID 是否已被处理，防止重复提交。
/// 命令需要实现 <see cref="IIdempotentRequest"/> 接口才会被拦截。
/// </summary>
public class IdempotencyBehavior<TRequest, TResponse>(
    IRedisCacheService redisCache,
    ILogger<IdempotencyBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private const string IdempotencyKeyPrefix = "idempotency:video";
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromHours(24);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    public async Task<TResponse> Handler(TRequest request, Func<Task<TResponse>> next,
        CancellationToken cancellationToken)
    {
        if (request is not IIdempotentRequest idempotent)
            return await next();

        var key = $"{IdempotencyKeyPrefix}:{typeof(TRequest).Name}:{idempotent.RequestId}";

        var cachedJson = await redisCache.StringGetAsync(key, cancellationToken);
        if (!string.IsNullOrEmpty(cachedJson))
        {
            var cachedResult = JsonSerializer.Deserialize<IdempotentResult<TResponse>>(cachedJson, JsonOptions);
            if (cachedResult is not null)
            {
                logger.LogWarning("Duplicate request detected: {RequestType} RequestId={RequestId}",
                    typeof(TRequest).Name, idempotent.RequestId);
                return cachedResult.Response;
            }
        }

        var response = await next();

        var result = new IdempotentResult<TResponse>(response, DateTime.UtcNow);
        var resultJson = JsonSerializer.Serialize(result, JsonOptions);
        await redisCache.StringSetAsync(key, resultJson, DefaultTtl, cancellationToken);

        logger.LogInformation("Idempotency key stored: {RequestType} RequestId={RequestId}",
            typeof(TRequest).Name, idempotent.RequestId);

        return response;
    }
}

/// <summary>标记命令需要幂等性保护。</summary>
public interface IIdempotentRequest
{
    Guid RequestId { get; }
}

/// <summary>幂等性缓存结果。</summary>
internal record IdempotentResult<T>(T Response, DateTime ProcessedAt);
