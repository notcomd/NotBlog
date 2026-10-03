namespace Message.Infrastructure.Services;

/// <summary>
/// 敏感词过滤默认实现 — 仅记录日志，直接放行
/// 后续可替换为接入第三方敏感词库的实现
/// </summary>
public class DefaultSensitiveWordFilter : ISensitiveWordFilter
{
    private readonly ILogger<DefaultSensitiveWordFilter> _logger;

    /// <summary>初始化 <see cref="DefaultSensitiveWordFilter"/> 实例。</summary>
    /// <param name="logger">日志记录器。</param>
    public DefaultSensitiveWordFilter(ILogger<DefaultSensitiveWordFilter> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>对内容进行敏感词过滤；当前默认实现仅记录日志并直接放行。</summary>
    public Task<FilterResult> FilterAsync(string content)
    {
        _logger.LogInformation("[预留] 敏感词过滤: 当前跳过检测, ContentLength={Length}", content?.Length ?? 0);
        return Task.FromResult(new FilterResult(true, Array.Empty<string>()));
    }
}
