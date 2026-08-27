namespace Markdown.Web.API.Services;

/// <summary>
///     本地磁盘 Markdown 正文存储（开发/单机实现，阶段 1）。
///     文件存放于 ContentRoot/markdown-files/{guid:N}.md；
///     阶段 2 替换为 FileDev gRPC 实现（IMarkdownContentStore 接口不变，只换 DI 注册）。
/// </summary>
public class LocalMarkdownContentStore : IMarkdownContentStore
{
    private readonly string _root;
    private readonly ILogger<LocalMarkdownContentStore> _logger;

    public LocalMarkdownContentStore(IWebHostEnvironment env, ILogger<LocalMarkdownContentStore> logger)
    {
        _root = Path.Combine(env.ContentRootPath, "markdown-files");
        Directory.CreateDirectory(_root);
        _logger = logger;
    }

    public Task<string> SaveAsync(string content, Guid? contentId = null, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        var fileId = $"{Guid.CreateVersion7():N}.md";
        var path = Path.Combine(_root, fileId);
        File.WriteAllText(path, content, Encoding.UTF8);
        _logger.LogDebug("本地存储 markdown 正文文件：{FileId}", fileId);
        return Task.FromResult(fileId);
    }

    public Task<string?> ReadAsync(string fileId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(fileId))
            return Task.FromResult<string?>(null);

        // 仅取文件名部分，防目录穿越（FileId 由本服务生成或来自文件存储后端）
        var path = Path.Combine(_root, Path.GetFileName(fileId));
        if (!File.Exists(path))
            return Task.FromResult<string?>(null);

        return Task.FromResult<string?>(File.ReadAllText(path, Encoding.UTF8));
    }

    public Task DeleteAsync(string fileId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(fileId))
            return Task.CompletedTask;

        var path = Path.Combine(_root, Path.GetFileName(fileId));
        if (File.Exists(path))
        {
            File.Delete(path);
            _logger.LogDebug("已删除本地 markdown 正文文件：{FileId}", fileId);
        }

        return Task.CompletedTask;
    }
}
