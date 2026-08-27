namespace FileDev.Domain.Enum;

/// <summary>
/// 默认标签的种类标识。
/// <para>
/// 用于承载"图片/文档/文件/视频"四类系统预置标签的稳定语义：
/// 即使默认标签被用户改名，仍能通过该种类在文件上传时完成自动归类。
/// <see cref="None"/> 表示用户自定义标签。
/// </para>
/// </summary>
public enum TagDefaultKind
{
    /// <summary>用户自定义标签（非系统预置）。</summary>
    None = 0,

    /// <summary>默认标签：图片。</summary>
    Image = 1,

    /// <summary>默认标签：文档。</summary>
    Document = 2,

    /// <summary>默认标签：文件（兜底分类）。</summary>
    File = 3,

    /// <summary>默认标签：视频。</summary>
    Video = 4,
}