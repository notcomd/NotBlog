namespace FileDev.Domain.Enum;

/// <summary>
/// 产生附件引用的业务内容类型，用于标识附件出自发帖 / Markdown / 视频等上游业务域。
/// 与 <see cref="FileSource.ContentAttachment"/> 配合描述附件的归属上下文。
/// </summary>
public enum ContentType
{
    /// <summary>用户文件仓库（非附件，占位，通常不用）。</summary>
    None = 0,

    /// <summary>发帖（Post）附件。</summary>
    Post = 1,

    /// <summary>Markdown / 文档附件。</summary>
    Markdown = 2,

    /// <summary>视频附件。</summary>
    Video = 3
}