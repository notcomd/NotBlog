namespace Message.Web.API.Dto.Response;

public class MessageDto
{
    public Guid MessageId { get; init; }
    public Guid SessionId { get; init; }
    public Guid SenderId { get; init; }
    public Guid? ReceiverId { get; init; }
    public MessageType MessageType { get; init; }
    public MessageStatus Status { get; init; }
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
    public DateTime SentTime { get; init; }
    public DateTime? DeliveredTime { get; init; }
    public DateTime? ReadTime { get; init; }
    public bool IsRecalled { get; init; }
    public bool IsForwarded { get; init; }
    public Guid? OriginalMessageId { get; init; }
    public Guid? ReplyToMessageId { get; init; }
    public List<FileAttachmentDto>? Attachments { get; init; }
}

public class FileAttachmentDto
{
    public Guid AttachmentId { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string FileType { get; init; } = string.Empty;
    public long FileSize { get; init; }
    public string FileUrl { get; init; } = string.Empty;
    public string? ThumbnailUrl { get; init; }
    public DateTime UploadTime { get; init; }
    public int DownloadCount { get; init; }
}

public class SessionDto
{
    public Guid SessionId { get; init; }
    public SessionType SessionType { get; init; }
    public string? SessionName { get; init; }
    public Guid? GroupId { get; init; }
    public Guid CreatorId { get; init; }
    public List<Guid> Participants { get; init; } = new();
    public Guid? LastMessageId { get; init; }
    public string? LastMessageContent { get; init; }
    public DateTime? LastMessageTime { get; init; }
    public int UnreadCount { get; init; }
    public DateTime CreatedTime { get; init; }
    public bool IsPinned { get; init; }
    public bool IsMuted { get; init; }
}

public class FriendDto
{
    public Guid FriendshipId { get; init; }
    public Guid FriendId { get; init; }
    public string? FriendName { get; init; }
    public string? FriendAvatar { get; init; }
    public FriendshipStatus Status { get; init; }
    public string? Remark { get; init; }
    public string? FriendGroupName { get; init; }
    public bool IsBlocked { get; init; }
    public bool IsMuted { get; init; }
    public bool IsStarred { get; init; }
    public DateTime CreatedTime { get; init; }
    public DateTime? LastInteractionTime { get; init; }
}

public class FriendRequestDto
{
    public Guid FriendshipId { get; init; }
    public Guid RequesterId { get; init; }
    public string? RequesterName { get; init; }
    public string? RequesterAvatar { get; init; }
    public FriendshipStatus Status { get; init; }
    public DateTime CreatedTime { get; init; }
}

public class GroupDto
{
    public Guid GroupId { get; init; }
    public string GroupName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Guid OwnerId { get; init; }
    public int MaxMembers { get; init; }
    public int MemberCount { get; init; }
    public bool IsPublic { get; init; }
    public DateTime CreatedTime { get; init; }
    public bool IsDismissed { get; init; }
}

public class GroupMemberDto
{
    public Guid MemberId { get; init; }
    public Guid GroupId { get; init; }
    public Guid UserId { get; init; }
    public string? UserName { get; init; }
    public string? UserAvatar { get; init; }
    public GroupMemberRole Role { get; init; }
    public string? Nickname { get; init; }
    public DateTime JoinTime { get; init; }
    public bool IsMuted { get; init; }
    public bool IsBanned { get; init; }
}

public class UserDto
{
    public Guid UserId { get; init; }
    public string UserName { get; init; } = string.Empty;
    public string? NickName { get; init; }
    public string? Avatar { get; init; }
    public UserStatus Status { get; init; }
    public DateTime? LastOnlineTime { get; init; }
}

public class PagedResult<T>
{
    public List<T> Items { get; init; } = new();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;

    public int Total { get; internal set; }
}

public class UnreadCountDto
{
    public Guid SessionId { get; init; }
    public int UnreadCount { get; init; }
}

public class OnlineStatusDto
{
    public Guid UserId { get; init; }
    public bool IsOnline { get; init; }
    public DateTime? LastOnlineTime { get; init; }
}