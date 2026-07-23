namespace Message.Web.API.Dto.Request;

public class SendMessageRequest
{
    public Guid SessionId { get; init; }
    public MessageType MessageType { get; init; }
    public string? Content { get; init; }
    public string? MediaUrl { get; init; }
    public string? ThumbnailUrl { get; init; }
    public string? FileName { get; init; }
    public long? FileSize { get; init; }
    public string? MimeType { get; init; }
    public double? Duration { get; init; }
    public string? Caption { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public string? LocationName { get; init; }
    public string? LinkUrl { get; init; }
    public string? LinkTitle { get; init; }
    public string? LinkDescription { get; init; }
    public string? ExpressionCode { get; init; }
    public Guid? ReplyToMessageId { get; init; }
}

public class ForwardMessageRequest
{
    public Guid TargetSessionId { get; init; }
    public ForwardType ForwardType { get; init; }
    public string? Comment { get; init; }
}

public class CreateSessionRequest
{
    public SessionType SessionType { get; init; }
    public Guid? FriendId { get; init; }
    public Guid? GroupId { get; init; }
    public string? SessionName { get; init; }
    public HashSet<Guid>? InitialMembers { get; init; }
}

public class SendFriendRequestRequest
{
    public Guid FriendId { get; init; }
    public string? Message { get; init; }
}

public class HandleFriendRequestRequest
{
    public bool Accept { get; init; }
}

public class UpdateFriendRemarkRequest
{
    public string Remark { get; init; } = string.Empty;
}

public class CreateGroupRequest
{
    public string GroupName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int MaxMembers { get; init; } = 500;
    public bool IsPublic { get; init; }
    public HashSet<Guid>? InitialMembers { get; init; }
}

public class AddGroupMemberRequest
{
    public Guid UserId { get; init; }
    public GroupMemberRole Role { get; init; } = GroupMemberRole.Member;
}

public class TransferOwnershipRequest
{
    public Guid NewOwnerId { get; init; }
}

public class MuteMemberRequest
{
    public int DurationMinutes { get; init; }
}

public class UpdateGroupInfoRequest
{
    public string GroupName { get; init; } = string.Empty;
    public string? Description { get; init; }
}

public class SetAdminRequest
{
    public Guid UserId { get; init; }
    public bool IsAdmin { get; init; }
}

public class UploadFileRequest
{
    public Guid MessageId { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string FileType { get; init; } = string.Empty;
    public long FileSize { get; init; }
    public string FileUrl { get; init; } = string.Empty;
}