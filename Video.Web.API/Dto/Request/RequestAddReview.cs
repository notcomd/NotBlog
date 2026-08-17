
namespace Video.Web.API.Dto.Request;

/// <summary>
/// 添加视频评论请求
/// ContentType: 可选，指定内容类型 (text/image/video/richtext)，不传则自动推断
/// </summary>
public record RequestAddReview(
    Guid UserGuid,
    Guid VideoGuid,
    string? Body,
    Guid? RootReview,
    List<VideoImage>? VideoImages,
    string? ContentType = null);