namespace Video.Domain.Entities;

/// <summary>
/// 视频权限
/// </summary>
public enum AuthorVideo
{
    /// <summary>
    /// 视频公开
    /// </summary>
    VideoPublic,

    /// <summary>
    /// 视频私密
    /// </summary>
    VideoPrivate,

    /// <summary>
    /// 视频保护
    /// </summary>
    VideoProtected
}