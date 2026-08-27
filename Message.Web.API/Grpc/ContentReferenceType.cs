namespace Message.Web.API.Grpc;

/// <summary>
/// 内容附件引用类型，用于在上传小文件/图片/分片合并时透传给 FileDev，
/// 用以建立内容附件弱引用（ContentAttachmentRef），将文件归属到具体业务内容（帖子/Markdown/视频）。
/// <para>
/// 该枚举为应用层语义，映射到 gRPC 契约的 <c>CONTENT_TYPE_*</c>，避免在业务层直接依赖 proto 枚举。
/// </para>
/// </summary>
public enum ContentReferenceType
{
    /// <summary>发帖（推文/圈子帖）内容附件</summary>
    Post = 1,

    /// <summary>Markdown 文档附件</summary>
    Markdown = 2,

    /// <summary>视频内容附件（文件+封面）</summary>
    Video = 3
}