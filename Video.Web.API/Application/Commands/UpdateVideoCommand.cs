using NotMediator;
using Video.Domain.Cache;
using Video.Domain.IRepository;
using Video.Domain.ValueObjects;

namespace Video.Web.API.Application.Commands;

/// <summary>更新视频元数据命令。</summary>
public record UpdateVideoCommand(
    Guid VideoGuid,
    string VideoName,
    string BriefIntroduction,
    Uri VideoCover,
    Uri VideoFileUri,    
    HashSet<string> Tags,
    VideoControl VideoControl) : IRequest<bool>{
        public DateTime CreatedAt => DateTime.Now;
    }


