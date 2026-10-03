namespace Message.Infrastructure.Services;

/// <summary>
/// 本地化默认实现 — 直接返回 key（中文硬编码模式）
/// 后续可替换为接入资源文件的实现
/// </summary>
public class DefaultLocalizationService : ILocalizationService
{
    /// <summary>按 key 返回本地化文本；当前默认实现直接返回 key（无参数时）或对 key 做格式化（有参数时）。</summary>
    public string Get(string key, params object[] args)
    {
        // 当前直接返回 key + args 的格式化结果
        return args.Length > 0 ? string.Format(key, args) : key;
    }
}
