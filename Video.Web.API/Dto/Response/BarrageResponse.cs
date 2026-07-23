namespace Video.Web.API.Dto.Response;

public record BarrageResponse(
    Guid VideoBarrageGuid,
    Guid VideoGuid,
    Guid UserGuid,
    string VideoBarrageBody,
    DateTimeOffset CreateAt,
    bool IsDelete);
