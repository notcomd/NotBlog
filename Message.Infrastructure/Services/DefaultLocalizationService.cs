using Message.Domain.IServices;

namespace Message.Infrastructure.Services;

/// <summary>
/// 本地化默认实现 — 直接返回 key（中文硬编码模式）
/// 后续可替换为接入资源文件的实现
/// </summary>
public class DefaultLocalizationService : ILocalizationService
{
    public string Get(string key, params object[] args)
    {
        // 当前直接返回 key + args 的格式化结果
        return args.Length > 0 ? string.Format(key, args) : key;
    }
}
