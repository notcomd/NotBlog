namespace FileDev.Web.API.Application.Commands;

/// <summary>图片上传结果（含尺寸与格式，供 gRPC 响应映射）</summary>
public record UploadImageResult(NotFile File, int Width, int Height, string Format);
