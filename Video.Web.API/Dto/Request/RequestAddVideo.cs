namespace Video.Web.API.Dto.Request;

public record RequestAddVideo(
    string VideoName,
    string BriefIntroduction,
    HashSet<string> Tags,
    HashSet<Guid> AffiliatedAuthorizes);
