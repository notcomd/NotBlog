namespace Video.Web.API.Dto.Request;

public record RequestUpdateByVideo(
    Guid VideoGuid,
    Guid AffiliatedUserGuid,
    string VideoName,
    Uri VideoCover,
    HashSet<string> Tags,
    string BriefIntroduction);