namespace FileDev.Domain;

using FileDev.Domain.Enum;

/// <summary>
/// 默认标签定义与文件类型自动归类规则。
/// <para>
/// 新用户注册时预置 <see cref="DefaultKinds"/> 四个默认标签（图片/文档/文件/视频）；
/// 上传文件时按扩展名通过 <see cref="ResolveDefaultKind"/> 归类到对应默认标签。
/// 归类规则为单源事实：此处定义扩展名 → 默认标签种类名 的唯一映射。
/// </para>
/// </summary>
public static class FileTagDefaults
{
    /// <summary>默认标签：图片</summary>
    public const string Images = "图片";

    /// <summary>默认标签：文档</summary>
    public const string Documents = "文档";

    /// <summary>默认标签：文件（兜底分类）</summary>
    public const string Files = "文件";

    /// <summary>默认标签：视频</summary>
    public const string Videos = "视频";

    /// <summary>默认标签种类 → 默认名称 的映射（顺序即预置插入顺序）。</summary>
    public static readonly IReadOnlyDictionary<TagDefaultKind, string> DefaultKindNames =
        new Dictionary<TagDefaultKind, string>
        {
            [TagDefaultKind.Image] = Images,
            [TagDefaultKind.Document] = Documents,
            [TagDefaultKind.File] = Files,
            [TagDefaultKind.Video] = Videos,
        };

    /// <summary>系统预置的默认标签种类集合（顺序即预置插入顺序）。</summary>
    public static readonly IReadOnlyList<TagDefaultKind> DefaultKinds =
        [TagDefaultKind.Image, TagDefaultKind.Document, TagDefaultKind.File, TagDefaultKind.Video];

    /// <summary>扩展名(小写，含点) → 默认标签种类 的归类映射。</summary>
    private static readonly IReadOnlyDictionary<string, TagDefaultKind> ExtensionMap =
        new Dictionary<string, TagDefaultKind>(StringComparer.OrdinalIgnoreCase)
        {
            // 图片
            [".jpg"] = TagDefaultKind.Image, [".jpeg"] = TagDefaultKind.Image, [".png"] = TagDefaultKind.Image,
            [".gif"] = TagDefaultKind.Image, [".bmp"] = TagDefaultKind.Image, [".webp"] = TagDefaultKind.Image,
            [".ico"] = TagDefaultKind.Image,
            // 文档
            [".pdf"] = TagDefaultKind.Document, [".doc"] = TagDefaultKind.Document, [".docx"] = TagDefaultKind.Document,
            [".xls"] = TagDefaultKind.Document, [".xlsx"] = TagDefaultKind.Document, [".ppt"] = TagDefaultKind.Document,
            [".pptx"] = TagDefaultKind.Document, [".txt"] = TagDefaultKind.Document, [".md"] = TagDefaultKind.Document,
            [".csv"] = TagDefaultKind.Document, [".json"] = TagDefaultKind.Document, [".xml"] = TagDefaultKind.Document,
            // 视频
            [".mp4"] = TagDefaultKind.Video, [".avi"] = TagDefaultKind.Video, [".mkv"] = TagDefaultKind.Video,
            [".mov"] = TagDefaultKind.Video, [".wmv"] = TagDefaultKind.Video, [".flv"] = TagDefaultKind.Video,
            [".webm"] = TagDefaultKind.Video,
        };

    /// <summary>
    /// 根据文件名（或直接传扩展名）解析应归入的默认标签种类。
    /// 命中扩展名映射返回对应种类，其余一律归入兜底种类 <see cref="TagDefaultKind.File"/>。
    /// </summary>
    public static TagDefaultKind ResolveDefaultKind(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return TagDefaultKind.File;

        var ext = Path.GetExtension(fileName);
        return ExtensionMap.TryGetValue(ext, out var kind) ? kind : TagDefaultKind.File;
    }

    /// <summary>获取默认标签种类的默认名称。</summary>
    public static string GetDefaultName(TagDefaultKind kind)
        => DefaultKindNames.TryGetValue(kind, out var name) ? name : Files;
}