namespace Message.Web.API.Dto.Request;

/// <summary>生成邀请：type = code | link | direct（direct 必须带 inviteeGuid）</summary>
public class GenerateInvitationRequest
{
    public string Type { get; init; } = "code";
    public Guid? InviteeGuid { get; init; }
    /// <summary>有效期（小时），默认 168（7 天）</summary>
    public int? TtlHours { get; init; }
}

