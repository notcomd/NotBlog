namespace Message.Domain.IServices;

/// <summary>
/// 本地化服务接口（预留）
/// 当前使用中文硬编码，后续可接入国际化
/// </summary>
public interface ILocalizationService
{
    /// <summary>按 key 获取本地化文本，并用可选参数格式化占位符</summary>
    string Get(string key, params object[] args);
}
