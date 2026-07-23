namespace Video.Web.API.Dto.Response;

public record VideoResponse(
    Guid VideoGuid,
    string VideoName,
    string BriefIntroduction,
    Uri VideoCover,
    Uri VideoFileUri,
    string VideoNvid,
    HashSet<string> VideoTags,
    long Upvote,
    long Stars,
    long Watch,
    long Down,
    long Ballot,
    long Share,
    bool IsDeleted,
    bool IsDisplayed,
    string AuthorVideo,
    string BarrageControl,
    DateTimeOffset CreateAt,
    DateTimeOffset UpdateAt);
