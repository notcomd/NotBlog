namespace FileDev.Web.API.Application.Queries;

/// <summary>文件下载结果（实体元数据 + 物理内容流，供 gRPC 流式响应）</summary>
public record FileDownloadResult(NotFile File, Stream Content);
