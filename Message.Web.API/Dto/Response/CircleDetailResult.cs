namespace Message.Web.API.Dto.Response;

/// <summary>圈子详情结果</summary>
public record CircleDetailResult(Circle? Circle, bool IsMember, CircleMemberRole? MyRole);
