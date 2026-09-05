namespace FileDev.Domain.Enum;

/// <summary>
/// 文件的来源域，用于区分用户文件仓库与业务内容附件。
/// 粗分二值：具体是发帖 / Markdown / 视频等粒度由 <see cref="Entities.ContentAttachmentRef.ContentType"/> 描述。
/// </summary>
public enum FileSource
{
    /// <summary>用户文件仓库：用户自行管理的个人文件（默认值），出现在用户的文件列表与网盘视图。</summary>
    UserRepository = 0,

    /// <summary>内容附件：由发帖 / Markdown / 视频等业务内容引用产生的资源文件，随业务生命周期回收。</summary>
    ContentAttachment = 1
}