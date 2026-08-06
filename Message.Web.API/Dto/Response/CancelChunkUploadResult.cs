namespace Message.Web.API.Dto.Response;
/// <summary>取消分片上传结果</summary>
public record CancelChunkUploadResult(bool Success, string? ErrorMessage);

