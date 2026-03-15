namespace Video.Web.API.VideosRequest;

public record DtoByUpdateVideo(
    Guid VideoGuid,
    Guid AffiliatedUserGuid,
    string VideoName,
    Uri VideoCover,
    HashSet<string> Tags,
    string BriefIntroduction);