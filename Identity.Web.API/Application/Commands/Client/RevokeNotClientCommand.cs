using Identity.Web.API.Application.Commands;

namespace Identity.Web.API.Application.Commands.Client;

/// <summary>
/// 吊销 OAuth 客户端（密钥泄露等场景；吊销后不可恢复，需重新创建）
/// </summary>
public record RevokeNotClientCommand(Guid NotClientId) : IRequest<bool>, ILoggableCommand
{
    public string IdProperty => nameof(NotClientId);
    public string IdValue => NotClientId.ToString();
}
