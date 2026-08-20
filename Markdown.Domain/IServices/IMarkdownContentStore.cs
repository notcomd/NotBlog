namespace Markdown.Domain.IServices;

/// <summary>
///     Markdown 文档正文内容存储抽象（文件化存储）。
///     数据库只保存文件元数据（FileId/FileUri/FileSize/FileExt），正文通过本接口读写。
///     当前实现：本地磁盘（开发，LocalMarkdownContentStore）；
///     生产实现：FileDev 文件服务（gRPC，阶段 2 接入）。
/// </summary>
public interface IMarkdownContentStore
{
    /// <summary>
    ///     保存文档正文内容，返回文件标识（FileId）
    /// </summary>
    /// <param name="content">正文内容</param>
    Task<string> SaveAsync(string content, CancellationToken ct = default);

    /// <summary>
    ///     读取文档正文内容（文件不存在返回 null）
    /// </summary>
    Task<string?> ReadAsync(string fileId, CancellationToken ct = default);

    /// <summary>
    ///     删除文档正文文件（不存在时静默成功）
    /// </summary>
    Task DeleteAsync(string fileId, CancellationToken ct = default);
}
