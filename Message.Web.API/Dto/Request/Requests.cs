namespace Message.Web.API.Dto.Request;

public class SendMessageRequest
{
    public Guid SessionId { get; init; }
    public MessageType MessageType { get; init; }
    public string? Content { get; init; }

    /// <summary>FileDev 文件 ID（图片/视频/音频/文件消息必填，来自上传接口返回的 FileRef.FileId）</summary>
    public Guid? FileId { get; init; }

    /// <summary>缩略图 FileDev 文件 ID（图片/视频消息可选）</summary>
    public Guid? ThumbnailFileId { get; init; }

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

// ─────────────────────────────────────────────────────────
// 大文件分片上传（断点续传）请求模型。
// 同时供 FilesApi（REST）与 MessageHub（SignalR）两种通道使用，
// 内部均通过 FileDev 的 gRPC 服务完成分片上传。
// ─────────────────────────────────────────────────────────

/// <summary>初始化分片上传请求</summary>
public class ChunkUploadInitRequest
{
    /// <summary>文件名（含扩展名）</summary>
    public string FileName { get; init; } = string.Empty;

    /// <summary>文件总大小（字节）</summary>
    public long TotalSize { get; init; }

    /// <summary>文件整体 MD5（可选，用于完整性校验与秒传）</summary>
    public string? FileMd5 { get; init; }

    /// <summary>是否公开文件（默认私有）</summary>
    public bool IsPublic { get; init; }

    /// <summary>文件描述</summary>
    public string? Description { get; init; }
}

/// <summary>上传单个分片请求</summary>
public class ChunkUploadRequest
{
    /// <summary>分片上传记录键（由初始化接口返回）</summary>
    public string FileKey { get; init; } = string.Empty;

    /// <summary>分片索引（从0开始）</summary>
    public int ChunkIndex { get; init; }

    /// <summary>分片二进制数据</summary>
    public byte[] ChunkData { get; init; } = [];

    /// <summary>分片 MD5（可选，用于服务端一致性校验）</summary>
    public string? ChunkMd5 { get; init; }
}

/// <summary>查询分片上传状态请求（用于断点续传）</summary>
public class ChunkStatusRequest
{
    /// <summary>分片上传记录键</summary>
    public string FileKey { get; init; } = string.Empty;
}

/// <summary>合并分片请求</summary>
public class ChunkMergeRequest
{
    /// <summary>分片上传记录键</summary>
    public string FileKey { get; init; } = string.Empty;

    /// <summary>最终文件名（可选，默认使用初始化时的文件名）</summary>
    public string? FileName { get; init; }

    /// <summary>文件描述</summary>
    public string? Description { get; init; }
}

/// <summary>取消分片上传请求</summary>
public class ChunkCancelRequest
{
    /// <summary>分片上传记录键</summary>
    public string FileKey { get; init; } = string.Empty;
}

/// <summary>
/// 断点续传请求：一次性提交缺失分片集合，
/// 服务端查询已上传分片后仅上传缺失部分，并实时推送上传进度。
/// </summary>
public class ChunkResumeRequest
{
    /// <summary>分片上传记录键</summary>
    public string FileKey { get; init; } = string.Empty;

    /// <summary>总分片数</summary>
    public int TotalChunks { get; init; }

    /// <summary>单个分片大小（字节），用于与服务端分片参数核对</summary>
    public int ChunkSize { get; init; }

    /// <summary>待上传分片集合（分片索引 → 分片二进制数据）</summary>
    public Dictionary<int, byte[]> Chunks { get; init; } = [];
}

/// <summary>给已发送消息补附件请求</summary>
public class AddAttachmentRequest
{
    /// <summary>FileDev 文件 ID（来自上传接口返回的 FileRef.FileId）</summary>
    public Guid FileId { get; init; }
}
