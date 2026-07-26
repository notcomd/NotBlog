

using NotMediator;
using Video.Domain.ValueObjects;
namespace Video.Web.API.Application.Commands;

/// <summary>
/// 上传视频命令
/// </summary>
/// <param name="AffectedUserGuid">视频所属人Guid列表</param>
/// <param name="VideoName">视频名称</param>
/// <param name="BriefIntroduction">视频简介</param>
/// <param name="VideoCover">视频封面</param>
/// <param name="VideoFileUri">视频文件Uri</param>
/// <param name="Tags">视频标签</param>
/// <param name="VideoControl">视频控制权限</param>
/// <returns>是否成功上传视频</returns>
public record UploadVideoCommand(HashSet<Guid> AffectedUserGuid, string VideoName,
string BriefIntroduction, Uri VideoCover, Uri VideoFileUri, HashSet<string> Tags,
VideoControl VideoControl) : IRequest<bool>
{
    public TimeSpace TimeSpace => new(DateTime.UtcNow, DateTime.UtcNow);
    public DateTime CreatedAt => DateTime.Now;
}
