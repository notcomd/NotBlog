namespace Video.Web.API.Dto.Request;

public record RequestAddBarrage(
    Guid VideoGuid,
    Guid UserGuid,
    string VideoBarrageBody);
