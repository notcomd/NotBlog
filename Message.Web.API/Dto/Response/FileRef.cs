namespace Message.Web.API.Dto.Response;
/// <summary>
/// 文件上传/合并后的统一引用（消息与 Tweet 附件共用）。
/// <para>
/// 由 FileDev gRPC 服务返回的元数据组装；客户端持 FileId 即可在发送消息/推文时引用附件，
/// 服务端凭 FileId 回查 FileDev 校验归属并填充消息元数据。
/// </para>
/// </summary>
public record FileRef(
    Guid FileId,
    Uri FileUri,
    string FileName,
    long FileSize,
    string FileMd5,
    string MimeType,
    int? Width = null,
    int? Height = null);

