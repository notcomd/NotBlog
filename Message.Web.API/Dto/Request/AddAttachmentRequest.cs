namespace Message.Web.API.Dto.Request;
/// <summary>给已发送消息补附件请求</summary>
public class AddAttachmentRequest
{
    /// <summary>FileDev 文件 ID（来自上传接口返回的 FileRef.FileId）</summary>
    public Guid FileId { get; init; }
}
