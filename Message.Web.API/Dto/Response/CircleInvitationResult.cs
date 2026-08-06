namespace Message.Web.API.Dto.Response;

/// <summary>生成邀请的结果（邀请码/链接 token 必须回传客户端才能使用）</summary>
public record CircleInvitationResult(Guid InviteGuid, string? Code, Guid? Token);
