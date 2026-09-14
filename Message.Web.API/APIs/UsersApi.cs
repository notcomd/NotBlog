using CacheMemory.Core;

namespace Message.Web.API.APIs;

/// <summary>
/// 用户公开信息接口（静态函数模式 + CQRS）。
/// <para>个人主页聚合端点：资料（昵称/头像/bio）+ 关注/粉丝计数 + 作品数 + 获赞总数 + 是否已关注；
/// 以及添加好友前的用户查找端点（按邮箱/昵称精确匹配 + 按用户限流）。</para>
/// </summary>
public static class UsersApi
{
    /// <summary>查找用户限流：每用户每分钟最大次数（防账号枚举）</summary>
    private const int LookupRateLimitPerMinute = 20;

    /// <summary>查找用户限流 key 前缀</summary>
    private const string LookupRateLimitKeyPrefix = "user:lookup:rate:";

    /// <summary>查找关键词长度下限（单字符会放大枚举效率，故要求至少 2 个字符）</summary>
    private const int LookupMinKeywordLength = 2;

    /// <summary>查找关键词长度上限</summary>
    private const int LookupMaxKeywordLength = 100;

    /// <summary>查找结果条数上限（昵称允许重名，仅返回少量候选）</summary>
    private const int LookupMaxResult = 5;

    /// <summary>映射用户公开信息端点组</summary>
    public static RouteGroupBuilder MapUsersApi(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users")
            .WithTags("Users")
            .RequireAuthorization();

        // GET /{userGuid} — 用户公开信息（个人主页）
        group.MapGet("/{userGuid}", GetUserProfileAsync)
            .RequirePermission("api:userinfo:read")
            .WithSummary("用户公开信息")
            .WithDescription("获取指定用户的资料、关注/粉丝计数、作品数与获赞总数，以及当前登录用户是否已关注对方")
            .Produces<ApiResponseResult<UserProfileDto>>();

        // GET /lookup — 按邮箱或昵称精确查找用户（添加好友前置步骤；字面量段优先于 {userGuid} 匹配）
        group.MapGet("/lookup", LookupUsersAsync)
            .RequirePermission("api:userinfo:read")
            .WithSummary("查找用户")
            .WithDescription("按邮箱或昵称精确查找用户（忽略大小写），用于添加好友前的用户定位；" +
                             "仅返回用户标识/昵称/头像，按用户每分钟限流")
            .Produces<ApiResponseResult<IEnumerable<UserBriefDto>>>()
            .Produces<ApiResponseResult<IEnumerable<UserBriefDto>>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponseResult<IEnumerable<UserBriefDto>>>(StatusCodes.Status429TooManyRequests);

        return group;
    }

    private static async Task<IResult> GetUserProfileAsync(
        Guid userGuid,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var userId = currentUser.IsAuthenticated ? currentUser.GetUserId() : Guid.Empty;
            var dto = await mediator.SendAsync(new GetUserProfileQuery(userGuid, userId), ct);
            return Results.Ok(ApiResponseResult<UserProfileDto>.Ok(dto));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponseResult<UserProfileDto>.Error($"获取用户信息失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>
    /// 查找用户：身份取自 JWT（不信任请求体），关键词仅做精确匹配，未命中返回空集合（HTTP 200）。
    /// <para>安全设计：需认证 + <c>api:userinfo:read</c> 权限；关键词长度限制；按用户限流（Redis）；
    /// 响应不含邮箱等敏感字段，且不区分「不存在」与「无权限查看」，抑制账号枚举。</para>
    /// </summary>
    private static async Task<IResult> LookupUsersAsync(
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        [FromServices] IRedisCacheService redisCacheService,
        [FromServices] ILoggerFactory loggerFactory,
        [FromQuery] string? keyword,
        CancellationToken ct)
    {
        var trimmed = keyword?.Trim() ?? string.Empty;
        if (trimmed.Length is < LookupMinKeywordLength or > LookupMaxKeywordLength)
            return Results.Json(
                ApiResponseResult<IEnumerable<UserBriefDto>>.Failure(
                    $"查找关键词长度需在 {LookupMinKeywordLength}-{LookupMaxKeywordLength} 个字符之间", 400),
                statusCode: 400);

        var userId = currentUser.GetUserId();
        var logger = loggerFactory.CreateLogger(nameof(UsersApi));

        if (!await TryPassRateLimitAsync(redisCacheService, userId, logger, ct))
            return Results.Json(
                ApiResponseResult<IEnumerable<UserBriefDto>>.Failure("查找过于频繁，请稍后再试", 429),
                statusCode: 429);

        try
        {
            var users = await mediator.SendAsync(new LookupUsersQuery(trimmed, LookupMaxResult), ct);
            return Results.Ok(ApiResponseResult<IEnumerable<UserBriefDto>>.Ok(users));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "查找用户失败：UserId={UserId}", userId);
            return Results.Json(ApiResponseResult<IEnumerable<UserBriefDto>>.Error("查找用户失败"), statusCode: 500);
        }
    }

    /// <summary>
    /// 按用户限流（固定分钟窗，口径与 Identity 登录限流一致：StringIncrement + 首次写入设过期）。
    /// <para>Redis 不可用时告警并放行：查找是只读低危操作，且认证与精确匹配仍生效；
    /// 如需「限流不可用即拒绝」可改为返回 false。</para>
    /// </summary>
    private static async Task<bool> TryPassRateLimitAsync(
        IRedisCacheService redisCacheService, Guid userId, ILogger logger, CancellationToken ct)
    {
        try
        {
            var window = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmm");
            var key = $"{LookupRateLimitKeyPrefix}{userId:N}:{window}";

            var count = await redisCacheService.StringIncrementAsync(key, 1, ct);
            if (count == 1)
                await redisCacheService.KeyExpireAsync(key, TimeSpan.FromMinutes(1), ct);

            return count <= LookupRateLimitPerMinute;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "用户查找限流不可用（Redis 异常），本次放行：UserId={UserId}", userId);
            return true;
        }
    }
}
