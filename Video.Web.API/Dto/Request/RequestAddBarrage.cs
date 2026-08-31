
namespace Video.Web.API.Dto.Request;

public record RequestAddBarrage(
    Guid VideoGuid,
    Guid UserGuid,
    string? VideoBarrageBody,
    List<BarrageImageRequest>? VideoImages,
    long? TimeAt = null);

/// <summary>
/// 弹幕图片请求 DTO。
/// </summary>
public record BarrageImageRequest(
    Uri ImageUrl,
    int? Width = null,
    int? Height = null,
    string? Format = null,
    long? FileSize = null,
    Uri? ThumbnailUrl = null,
    string? Description = null,
    int SortOrder = 0);
