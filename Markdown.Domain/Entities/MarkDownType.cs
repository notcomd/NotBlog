namespace Markdown.Domain.Entities;

public enum MarkDownType
{
    /// <summary>
    /// 公开文档，所有人都可以查看
    /// </summary>
    PublicMark,

    /// <summary>
    /// 私有文档，只有创建者可以查看
    /// </summary>
    PrivateMark,

    /// <summary>
    /// 受保护文档，只有授权用户可以查看
    /// </summary>
    ProtectedMark,

    /// <summary>
    /// 管理员文档，只有管理员可以查看
    /// </summary>
    AdminMark,

    /// <summary>
    /// 根文档，只有根用户可以查看
    /// </summary>
    RootMark
}