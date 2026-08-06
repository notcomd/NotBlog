namespace Identity.Web.API.Application.Commands.Client;

/// <summary>
/// 创建客户端结果 — ClientSecret 仅在创建时返回一次，之后无法再查询
/// </summary>
public record CreateNotClientResult(Guid NotClientId, string ClientId, string ClientSecret);
