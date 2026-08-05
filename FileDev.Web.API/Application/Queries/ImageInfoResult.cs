namespace FileDev.Web.API.Application.Queries;

/// <summary>图片信息结果（含尺寸与格式，供 gRPC 响应映射）</summary>
public record ImageInfoResult(NotFile File, int Width, int Height, string Format);
