namespace Message.Domain.IServices;

/// <summary>
/// 本地化服务接口（预留）
/// 当前使用中文硬编码，后续可接入国际化
/// </summary>
public interface ILocalizationService
{
    string Get(string key, params object[] args);
}
