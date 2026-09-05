
namespace Video.Web.API.Dto.Response;

public record BarrageResponse(
    Guid VideoBarrageGuid,
    Guid VideoGuid,
    Guid UserGuid,
    string? VideoBarrageBody,
    string BarrageType,
    long? TimeAt,
    DateTimeOffset CreateAt,
    bool IsDelete,
    List<BarrageImageResponse>? VideoImages = null);

/// <summary>
/// 弹幕图片响应 DTO。
/// </summary>
public record BarrageImageResponse(
    Uri ImageUrl,
    Uri? ThumbnailUrl = null,
    int? Width = null,
    int? Height = null,
    string? Format = null,
    string? Description = null,
    int SortOrder = 0);
