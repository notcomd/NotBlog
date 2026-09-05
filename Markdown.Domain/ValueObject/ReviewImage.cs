namespace Markdown.Domain.Entities;

/// <summary>
///     评论配图（值对象）。
///     仅持有已上传资源的引用（图片 URL 与文件名），无独立身份与生命周期，
///     作为 <see cref="MarkReview.ReviewImages"/> 的 owned 集合整体序列化为 JSONB 列存储，
///     不建独立表、不产生外键（消除原独立实体表格设计与 Guid/int 外键类型不匹配的问题）。
/// </summary>
public class ReviewImage
{
    /// <summary>
    ///     私有构造函数，供 EF Core 反序列化 owned JSON 使用
    /// </summary>
    private ReviewImage()
    {
    }

    /// <summary>
    ///     创建评论配图值对象
    /// </summary>
    public ReviewImage(Uri imageUrl, string imageName)
    {
        ArgumentNullException.ThrowIfNull(imageUrl);
        ArgumentNullException.ThrowIfNull(imageName);

        ImageUrl = imageUrl;
        ImageName = imageName;
    }

    /// <summary>
    ///     图片访问 URI（已上传至文件存储后的绝对地址）
    /// </summary>
    public Uri ImageUrl { get; private set; } = null!;

    /// <summary>
    ///     图片文件名（用于显示/下载名）
    /// </summary>
    public string ImageName { get; private set; } = null!;

    /// <summary>
    ///     值对象相等性：ImageUrl 与 ImageName 均相等即视为同一张配图
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (obj is null || obj is not ReviewImage other)
            return false;

        return ImageUrl == other.ImageUrl && ImageName == other.ImageName;
    }

    public override int GetHashCode() => HashCode.Combine(ImageUrl, ImageName);
}