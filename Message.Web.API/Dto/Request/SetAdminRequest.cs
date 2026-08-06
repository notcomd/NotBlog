namespace Message.Web.API.Dto.Request;
public class SetAdminRequest
{
    public Guid UserId { get; init; }
    public bool IsAdmin { get; init; }
}

// ─────────────────────────────────────────────────────────
// 大文件分片上传（断点续传）请求模型。
// 同时供 FilesApi（REST）与 MessageHub（SignalR）两种通道使用，
// 内部均通过 FileDev 的 gRPC 服务完成分片上传。
// ─────────────────────────────────────────────────────────

