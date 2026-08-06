using MessageEntity = Message.Domain.Entities.Message;

namespace Message.Web.API.Application.Commands.Messages;
/// <summary>
/// 发送消息命令。
/// <para>CQRS 命令侧：仅返回新消息的标识（Guid），不返回业务实体/DTO。</para>
/// <para>
/// 重新设计（MessageApi-Redesign v2）：媒体消息（图片/视频/音频/文件）不再接受客户端提供的
/// 任意 URL/文件名/大小，改为携带 <see cref="FileId"/>（上传接口返回的 FileDev 文件 ID），
/// 服务端经 gRPC GetFileInfo 校验文件归属后统一填充消息媒体元数据，并同事务创建附件记录。
/// </para>
/// </summary>
/// <param name="SessionId">会话 ID</param>
/// <param name="SenderId">发送者用户 ID</param>
/// <param name="MessageType">消息类型</param>
/// <param name="Content">文本内容（文本消息必填）</param>
/// <param name="FileId">FileDev 文件 ID（图片/视频/音频/文件消息必填）</param>
/// <param name="ThumbnailFileId">缩略图 FileDev 文件 ID（图片/视频消息可选）</param>
/// <param name="Duration">媒体时长（视频/音频消息可选）</param>
/// <param name="Caption">媒体描述（图片/视频/音频消息可选）</param>
/// <param name="Latitude">纬度（位置消息必填）</param>
/// <param name="Longitude">经度（位置消息必填）</param>
/// <param name="LocationName">位置名称（位置消息必填）</param>
/// <param name="LinkUrl">链接地址（链接消息必填）</param>
/// <param name="LinkTitle">链接标题（链接消息可选）</param>
/// <param name="LinkDescription">链接描述（链接消息可选）</param>
/// <param name="ExpressionCode">表情代码（表情消息必填）</param>
public record SendMessageCommand(
    Guid SessionId,
    Guid SenderId,
    MessageType MessageType,
    string? Content,
    Guid? FileId,
    Guid? ThumbnailFileId,
    double? Duration,
    string? Caption,
    double? Latitude,
    double? Longitude,
    string? LocationName,
    string? LinkUrl,
    string? LinkTitle,
    string? LinkDescription,
    string? ExpressionCode) : IRequest<Guid>;

