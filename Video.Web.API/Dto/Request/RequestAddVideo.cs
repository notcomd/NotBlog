namespace Video.Web.API.Dto.Request;

/// <summary>新增视频请求（multipart/form-data）。</summary>
/// <param name="VideoName">视频名称</param>
/// <param name="BriefIntroduction">简介</param>
/// <param name="Tags">标签</param>
/// <param name="AffiliatedAuthorizes">作者授权（服务端忽略，改用当前登录用户）</param>
/// <param name="AsDraft">是否保存为草稿：null 视为 true（草稿）；false 时创建后立即提交审核（不直接发布）</param>
public record RequestAddVideo(
    string VideoName,
    string BriefIntroduction,
    HashSet<string> Tags,
    HashSet<Guid> AffiliatedAuthorizes,
    bool? AsDraft = null);
