
namespace Identity.Web.API.Application.IntegrationEvents.Events;

/// <summary>
/// 用户头像更新集成事件。
/// <para>
/// 携带 Email/NickName：Message 等下游若尚未建立 UserInfo 投影（注册事件丢失或消费顺序错乱），
/// 可凭这两个字段按注册语义补建资料，避免头像更新被静默丢弃。
/// </para>
/// </summary>
public record UploadByUserAvatarIntegrationEvent(
    Guid UserId,
    Uri AvatarUrl,
    string? Email = null,
    string? NickName = null) : IntegrationEvent
{
    public DateTime UploadTime { get; init; } = DateTime.UtcNow;
}
