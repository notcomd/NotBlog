using System.Security.Claims;
using FileDev.Web.API.Grpc;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.ClientFactory;
using Microsoft.Extensions.Options;
using Notcomd.Token.JWT.Core;

namespace Markdown.Web.API.Services;

/// <summary>
///     FileDev gRPC 正文存储实现（阶段 2 目标后端）。
///     <para>
///     文档文件以 FILE_PUBLIC 上传（FileDev 权限模型：非私有文件任何已认证调用者可下载），
///     内容权限由 Markdown 服务层把关（HasPermission + 审核门控，见 /content 端点）。
///     调用携带服务级 JWT（固定服务账号，共享 JWT_PRIVATE_KEY 签发、FileDev 同配置校验），
///     不依赖用户 token —— 匿名读取公开文档正文不受 gRPC 用户认证限制。
///     </para>
/// </summary>
public class FileDevMarkdownContentStore : IMarkdownContentStore
{
    /// <summary>Markdown 服务账号固定 ID（FileDev 侧视为文件归属者，上传/删除校验用）</summary>
    public static readonly Guid ServiceAccountId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    /// <summary>gRPC 客户端服务发现名（与 AppHost 服务名一致）</summary>
    public const string ClientName = "filedev-web-api";

    private readonly GrpcClientFactory _clientFactory;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IOptionsSnapshot<JwtOptions> _jwtOptions;
    private readonly ILogger<FileDevMarkdownContentStore> _logger;

    private string? _cachedToken;
    private DateTimeOffset _tokenExpiresAt;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    public FileDevMarkdownContentStore(
        GrpcClientFactory clientFactory,
        IJwtTokenService jwtTokenService,
        IOptionsSnapshot<JwtOptions> jwtOptions,
        ILogger<FileDevMarkdownContentStore> logger)
    {
        _clientFactory = clientFactory;
        _jwtTokenService = jwtTokenService;
        _jwtOptions = jwtOptions;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> SaveAsync(string content, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        var fileName = $"{Guid.CreateVersion7():N}.md";
        var request = new UploadFileRequest
        {
            UserId = ServiceAccountId.ToString(),
            FileName = fileName,
            FileContent = ByteString.CopyFromUtf8(content),
            FileIdentity = FileIdentity.FilePublic,
            FileDescription = "markdown 文档正文"
        };

        var response = await ExecuteWithRetryAsync(
            async client => await client.UploadFileAsync(request, await BuildCallOptionsAsync(ct)).ResponseAsync,
            "上传正文文件", ct);

        if (!response.Success)
            throw new InvalidOperationException($"正文文件上传失败: {response.ErrorMessage}");

        _logger.LogInformation("已上传 markdown 正文文件 {FileId}（{Size} 字节）", response.FileId, response.FileSize);
        return response.FileId;
    }

    /// <inheritdoc />
    public async Task<string?> ReadAsync(string fileId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(fileId))
            return null;

        try
        {
            var request = new DownloadFileRequest
            {
                FileId = fileId,
                UserId = ServiceAccountId.ToString()
            };

            var call = CreateClient().DownloadFile(request, await BuildCallOptionsAsync(ct));
            using var ms = new MemoryStream();
            await foreach (var chunk in call.ResponseStream.ReadAllAsync(ct))
            {
                ms.Write(chunk.ChunkData.ToByteArray());
            }

            return Encoding.UTF8.GetString(ms.ToArray());
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            _logger.LogWarning("正文文件不存在：{FileId}", fileId);
            return null;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "读取正文文件失败：{FileId}", fileId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string fileId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(fileId))
            return;

        try
        {
            var request = new DeleteFileRequest
            {
                FileId = fileId,
                UserId = ServiceAccountId.ToString()
            };

            var response = await ExecuteWithRetryAsync(
                async client => await client.DeleteFileAsync(request, await BuildCallOptionsAsync(ct)).ResponseAsync,
                "删除正文文件", ct);

            if (!response.Success)
                _logger.LogWarning("删除正文文件失败：{FileId}：{Error}", fileId, response.ErrorMessage);
        }
        catch (Exception ex)
        {
            // 删除失败不阻断业务（旧文件残留由存储侧清理策略兜底）
            _logger.LogWarning(ex, "删除正文文件异常：{FileId}", fileId);
        }
    }

    // ─────────────────────────── 私有辅助 ───────────────────────────

    /// <summary>
    ///     获取（缓存）服务级 JWT：固定服务账号，FileDev 同 JwtOptions 校验。
    ///     缓存至过期前 1 分钟自动刷新，多线程竞争由信号量串行化。
    /// </summary>
    private async Task<string> GetServiceTokenAsync(CancellationToken ct)
    {
        if (_cachedToken is not null && _tokenExpiresAt > DateTimeOffset.UtcNow.AddMinutes(1))
            return _cachedToken;

        await _tokenLock.WaitAsync(ct);
        try
        {
            if (_cachedToken is not null && _tokenExpiresAt > DateTimeOffset.UtcNow.AddMinutes(1))
                return _cachedToken;

            var result = await _jwtTokenService.BuildTokenAsync(
            [
                new Claim("id", ServiceAccountId.ToString()),
                new Claim(ClaimTypes.Role, "markdown-service")
            ], _jwtOptions.Value);

            _cachedToken = result.AccessToken;
            _tokenExpiresAt = result.ExpiresAt;
            _logger.LogDebug("已签发 markdown 服务 token，过期时间 {ExpiresAt}", _tokenExpiresAt);
            return _cachedToken;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    /// <summary>构建调用选项（服务 token + 超时 + 取消）</summary>
    private async Task<CallOptions> BuildCallOptionsAsync(CancellationToken ct)
    {
        var token = await GetServiceTokenAsync(ct);
        var headers = new Metadata
        {
            { "Authorization", $"Bearer {token}" }
        };

        return new CallOptions(
            headers: headers,
            cancellationToken: ct,
            deadline: DateTime.UtcNow.AddSeconds(120));
    }

    /// <summary>创建 gRPC 客户端（按服务发现名称解析）</summary>
    private FileStorage.FileStorageClient CreateClient()
        => _clientFactory.CreateClient<FileStorage.FileStorageClient>(ClientName);

    /// <summary>执行 gRPC 调用并针对瞬时故障进行指数退避重试</summary>
    private async Task<T> ExecuteWithRetryAsync<T>(
        Func<FileStorage.FileStorageClient, Task<T>> call,
        string operation, CancellationToken ct)
    {
        var attempt = 0;
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                return await call(CreateClient());
            }
            catch (RpcException ex) when (attempt < 3 && IsRetryable(ex.StatusCode))
            {
                attempt++;
                var delay = TimeSpan.FromMilliseconds(Math.Min(500 * Math.Pow(2, attempt - 1), 4000));
                _logger.LogWarning(ex,
                    "gRPC 调用 {Operation} 失败（{StatusCode}），第 {Attempt}/3 次重试",
                    operation, ex.StatusCode, attempt);
                await Task.Delay(delay, ct);
            }
        }
    }

    /// <summary>瞬时故障状态码（不含 NotFound：文件不存在属确定性失败）</summary>
    private static bool IsRetryable(StatusCode statusCode)
        => statusCode is StatusCode.Unavailable or StatusCode.DeadlineExceeded
            or StatusCode.Aborted or StatusCode.Internal;
}
