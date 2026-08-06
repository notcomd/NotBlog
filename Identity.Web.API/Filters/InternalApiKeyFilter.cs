namespace Identity.Web.API.Filters;

/// <summary>
/// 网关内部调用凭证过滤器（V4）
///
/// 校验请求头 X-Internal-Api-Key 与配置的 GatewayInternal:ApiKey
/// （或环境变量 GATEWAY_INTERNAL_API_KEY）一致。
///
/// 安全语义（fail-closed）：密钥未配置或校验失败一律 401 —— 网关专用端点
/// （mappings / check / check-and-scope / datascope）不应匿名可达。
/// </summary>
public sealed class InternalApiKeyFilter : IEndpointFilter
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<InternalApiKeyFilter> _logger;

    public InternalApiKeyFilter(IConfiguration configuration, ILogger<InternalApiKeyFilter> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var expected = _configuration["GatewayInternal:ApiKey"]
            ?? Environment.GetEnvironmentVariable("GATEWAY_INTERNAL_API_KEY");

        var provided = context.HttpContext.Request.Headers["X-Internal-Api-Key"].FirstOrDefault();

        if (string.IsNullOrEmpty(expected) || !string.Equals(provided, expected, StringComparison.Ordinal))
        {
            _logger.LogWarning(
                "[InternalApiKey] 内部凭证校验失败 Path={Path}（服务端已配置密钥={Configured}）",
                context.HttpContext.Request.Path, !string.IsNullOrEmpty(expected));
            return Results.Json(new { error = "Unauthorized" },
                statusCode: StatusCodes.Status401Unauthorized);
        }

        return await next(context);
    }
}
