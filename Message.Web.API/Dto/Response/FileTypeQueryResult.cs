namespace Message.Web.API.Dto.Response;

/// <summary>文件类型分类查询结果（File 为 null 表示文件不存在）</summary>
/// <param name="File">文件附件</param>
/// <param name="IsImage">是否为图片</param>
/// <param name="IsVideo">是否为视频</param>
/// <param name="IsAudio">是否为音频</param>
/// <param name="IsDocument">是否为文档</param>
public record FileTypeQueryResult(FileAttachment File, bool IsImage, bool IsVideo, bool IsAudio, bool IsDocument);

