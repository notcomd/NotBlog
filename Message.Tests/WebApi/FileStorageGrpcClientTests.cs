using FileDev.Web.API.Grpc;
using FileInfo = FileDev.Web.API.Grpc.FileInfo;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.ClientFactory;
using Message.Tests.TestHelpers;
using Message.Domain.IServices;
using Microsoft.AspNetCore.Http;
using Message.Web.API.Dto.Response;
using Message.Web.API.Grpc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Message.Tests.WebApi;

/// <summary>
/// 文件存储 gRPC 客户端（<see cref="FileStorageGrpcClient"/>）单元测试。
/// 覆盖：请求/响应映射、参数校验、瞬时故障指数退避重试、异常信息映射、
/// 断点续传（跳过已上传分片）与上传进度反馈。
/// </summary>
[TestFixture]
public class FileStorageGrpcClientTests
{
    private const string FileKey = "chunk-key-001";
    private static readonly Guid UserId = Guid.NewGuid();

    private Mock<FileStorage.FileStorageClient> _grpcClient = null!;
    private Mock<IHttpContextAccessor> _httpContextAccessor = null!;
    private FileStorageGrpcClient _client = null!;
    private FileStorageGrpcOptions _options = null!;

    [SetUp]
    public void Setup()
    {
        _options = new FileStorageGrpcOptions
        {
            ChunkSize = 1_048_576, // 1MB
            RetryCount = 3,
            TimeoutSeconds = 30
        };

        var options = new Mock<IOptionsSnapshot<FileStorageGrpcOptions>>();
        options.Setup(o => o.Value).Returns(() => _options);

        _httpContextAccessor = new Mock<IHttpContextAccessor>();

        _grpcClient = new Mock<FileStorage.FileStorageClient>(Mock.Of<CallInvoker>());

        _client = new FileStorageGrpcClient(
            new StubGrpcClientFactory(() => _grpcClient.Object),
            options.Object,
            new Mock<ILogger<FileStorageGrpcClient>>().Object,
            _httpContextAccessor.Object);
    }

    // ─────────────────────────── 上传文件 ───────────────────────────

    [Test]
    public async Task UploadFileAsync_成功响应应正确映射元数据()
    {
        var fileId = Guid.NewGuid();
        _grpcClient
            .Setup(c => c.UploadFileAsync(It.IsAny<UploadFileRequest>(), It.IsAny<CallOptions>()))
            .Returns(GrpcTestHelper.Success(new UploadFileResponse
            {
                Success = true,
                FileId = fileId.ToString(),
                FileUri = "files/abc.txt",
                FileMd5 = "md5-hash",
                FileSize = 1024
            }));

        var result = await _client.UploadFileAsync(UserId, "a.txt", new byte[] { 1, 2, 3 });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.FileId, Is.EqualTo(fileId));
            Assert.That(result.FileMd5, Is.EqualTo("md5-hash"));
            Assert.That(result.FileSize, Is.EqualTo(1024));
            Assert.That(result.ErrorMessage, Is.Null);
        });
    }

    [Test]
    public async Task UploadFileAsync_空内容应直接失败且不调用gRPC()
    {
        var result = await _client.UploadFileAsync(UserId, "a.txt", Array.Empty<byte>());

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("文件内容不能为空"));
        _grpcClient.Verify(c => c.UploadFileAsync(It.IsAny<UploadFileRequest>(), It.IsAny<CallOptions>()),
            Times.Never);
    }

    [Test]
    public async Task UploadFileAsync_grpc失败应映射为错误结果()
    {
        _options.RetryCount = 0; // 关闭重试，直接进入错误映射
        _grpcClient
            .Setup(c => c.UploadFileAsync(It.IsAny<UploadFileRequest>(), It.IsAny<CallOptions>()))
            .Returns(GrpcTestHelper.Throws<UploadFileResponse>(
                GrpcTestHelper.RpcError(StatusCode.InvalidArgument, "文件名非法")));

        var result = await _client.UploadFileAsync(UserId, "a.txt", new byte[] { 1 });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("文件名非法"));
        });
    }

    [Test]
    public async Task UploadFileAsync_瞬时故障应按指数退避重试()
    {
        var fileId = Guid.NewGuid();
        _grpcClient
            .SetupSequence(c => c.UploadFileAsync(It.IsAny<UploadFileRequest>(), It.IsAny<CallOptions>()))
            .Returns(GrpcTestHelper.Throws<UploadFileResponse>(
                GrpcTestHelper.RpcError(StatusCode.Unavailable, "服务暂不可用")))
            .Returns(GrpcTestHelper.Success(new UploadFileResponse
            {
                Success = true,
                FileId = fileId.ToString(),
                FileMd5 = "md5",
                FileSize = 3
            }));

        var result = await _client.UploadFileAsync(UserId, "a.txt", new byte[] { 1, 2, 3 });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, "重试后应成功");
            _grpcClient.Verify(c => c.UploadFileAsync(It.IsAny<UploadFileRequest>(), It.IsAny<CallOptions>()),
                Times.Exactly(2), "首次失败后应重试一次");
        });
    }

    [Test]
    public async Task UploadFileAsync_重试耗尽后应返回失败()
    {
        _options.RetryCount = 1;
        _grpcClient
            .Setup(c => c.UploadFileAsync(It.IsAny<UploadFileRequest>(), It.IsAny<CallOptions>()))
            .Returns(GrpcTestHelper.Throws<UploadFileResponse>(
                GrpcTestHelper.RpcError(StatusCode.Unavailable, "服务暂不可用")));

        var result = await _client.UploadFileAsync(UserId, "a.txt", new byte[] { 1 });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            _grpcClient.Verify(c => c.UploadFileAsync(It.IsAny<UploadFileRequest>(), It.IsAny<CallOptions>()),
                Times.Exactly(2), "初始调用 1 次 + 重试 1 次");
        });
    }

    // ─────────────────────────── 分片上传 ───────────────────────────

    [Test]
    public async Task InitChunkUploadAsync_应计算分片数并查询已上传分片()
    {
        _grpcClient
            .Setup(c => c.InitChunkUploadAsync(It.IsAny<InitChunkUploadRequest>(), It.IsAny<CallOptions>()))
            .Returns(GrpcTestHelper.Success(new InitChunkUploadResponse
            {
                Success = true,
                FileKey = FileKey,
                TotalChunks = 10,
                ChunkSize = 1_048_576
            }));
        _grpcClient
            .Setup(c => c.GetChunkStatusAsync(It.IsAny<GetChunkStatusRequest>(), It.IsAny<CallOptions>()))
            .Returns(GrpcTestHelper.Success(new GetChunkStatusResponse
            {
                Success = true,
                FileKey = FileKey,
                TotalChunks = 10,
                Status = ChunkUploadStatus.ChunkUploading
            }));

        // 10MB 文件 / 1MB 分片 = 10 个分片
        var result = await _client.InitChunkUploadAsync(UserId, "video.mp4", 10 * 1024 * 1024, "md5");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.FileKey, Is.EqualTo(FileKey));
            Assert.That(result.TotalChunks, Is.EqualTo(10));
            Assert.That(result.ChunkSize, Is.EqualTo(1_048_576));
            Assert.That(result.UploadedChunks, Is.Empty);
            _grpcClient.Verify(c => c.InitChunkUploadAsync(It.IsAny<InitChunkUploadRequest>(), It.IsAny<CallOptions>()),
                Times.Once);
            _grpcClient.Verify(c => c.GetChunkStatusAsync(It.IsAny<GetChunkStatusRequest>(), It.IsAny<CallOptions>()),
                Times.Once, "初始化后应查询已上传分片以支持断点续传");
        });
    }

    [Test]
    public async Task InitChunkUploadAsync_文件大小非法应直接失败()
    {
        var result = await _client.InitChunkUploadAsync(UserId, "a.txt", 0);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("文件大小必须大于0"));
        _grpcClient.Verify(c => c.InitChunkUploadAsync(It.IsAny<InitChunkUploadRequest>(), It.IsAny<CallOptions>()),
            Times.Never);
    }

    [Test]
    public async Task UploadChunkAsync_成功响应应映射分片结果()
    {
        _grpcClient
            .Setup(c => c.UploadChunkAsync(It.IsAny<UploadChunkRequest>(), It.IsAny<CallOptions>()))
            .Returns(GrpcTestHelper.Success(new UploadChunkResponse
            {
                Success = true,
                ChunkIndex = 3,
                ChunkMd5 = "chunk-md5"
            }));

        var result = await _client.UploadChunkAsync(FileKey, 3, new byte[] { 9, 9 });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.ChunkIndex, Is.EqualTo(3));
            Assert.That(result.ChunkMd5, Is.EqualTo("chunk-md5"));
        });
    }

    [Test]
    public async Task GetChunkStatusAsync_应映射已上传分片索引()
    {
        _grpcClient
            .Setup(c => c.GetChunkStatusAsync(It.IsAny<GetChunkStatusRequest>(), It.IsAny<CallOptions>()))
            .Returns(GrpcTestHelper.Success(new GetChunkStatusResponse
            {
                Success = true,
                FileKey = FileKey,
                TotalChunks = 4,
                Status = ChunkUploadStatus.ChunkUploading,
                UploadedChunks = { 0, 2 }
            }));

        var result = await _client.GetChunkStatusAsync(FileKey);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.TotalChunks, Is.EqualTo(4));
            Assert.That(result.UploadedChunks, Is.EquivalentTo(new[] { 0, 2 }));
            Assert.That(result.IsComplete, Is.False);
            Assert.That(result.Percent, Is.EqualTo(50));
        });
    }

    [Test]
    public async Task MergeChunksAsync_应映射最终文件元数据()
    {
        var fileId = Guid.NewGuid();
        _grpcClient
            .Setup(c => c.MergeChunksAsync(It.IsAny<MergeChunksRequest>(), It.IsAny<CallOptions>()))
            .Returns(GrpcTestHelper.Success(new MergeChunksResponse
            {
                Success = true,
                FileId = fileId.ToString(),
                FileMd5 = "merged-md5",
                FileSize = 100
            }));

        var result = await _client.MergeChunksAsync(FileKey, UserId, "a.mp4");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.FileId, Is.EqualTo(fileId));
            Assert.That(result.FileMd5, Is.EqualTo("merged-md5"));
        });
    }

    [Test]
    public async Task CancelChunkUploadAsync_空文件Key应直接失败()
    {
        var result = await _client.CancelChunkUploadAsync("");

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("文件Key不能为空"));
    }

    // ─────────────────────────── 断点续传 ───────────────────────────

    [Test]
    public async Task ResumeChunkUploadAsync_应跳过已上传分片并反馈进度()
    {
        // 服务端已有分片 0、1
        _grpcClient
            .Setup(c => c.GetChunkStatusAsync(It.IsAny<GetChunkStatusRequest>(), It.IsAny<CallOptions>()))
            .Returns(GrpcTestHelper.Success(new GetChunkStatusResponse
            {
                Success = true,
                FileKey = FileKey,
                TotalChunks = 4,
                Status = ChunkUploadStatus.ChunkUploading,
                UploadedChunks = { 0, 1 }
            }));
        // 分片上传一律成功，回显索引
        _grpcClient
            .Setup(c => c.UploadChunkAsync(It.IsAny<UploadChunkRequest>(), It.IsAny<CallOptions>()))
            .Returns((UploadChunkRequest req, CallOptions opts) =>
                GrpcTestHelper.Success(new UploadChunkResponse
                {
                    Success = true,
                    ChunkIndex = req.ChunkIndex
                }));

        var chunks = new Dictionary<int, byte[]>
        {
            [0] = new byte[] { 0 },
            [1] = new byte[] { 1 },
            [2] = new byte[] { 2 },
            [3] = new byte[] { 3 }
        };

        var progressReports = new List<ChunkUploadProgress>();
        var progress = new Progress<ChunkUploadProgress>(progressReports.Add);

        var result = await _client.ResumeChunkUploadAsync(FileKey, 4, 1024, chunks, progress);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.UploadedChunks, Is.EquivalentTo(new[] { 0, 1, 2, 3 }));
            // 仅上传缺失的分片 2、3
            _grpcClient.Verify(
                c => c.UploadChunkAsync(
                    It.Is<UploadChunkRequest>(r => r.ChunkIndex == 0 || r.ChunkIndex == 1),
                    It.IsAny<CallOptions>()),
                Times.Never, "已上传分片必须跳过");
            // 缺失分片 2、3 各上传一次
            _grpcClient.Verify(
                c => c.UploadChunkAsync(
                    It.Is<UploadChunkRequest>(r => r.ChunkIndex == 2),
                    It.IsAny<CallOptions>()),
                Times.Once, "缺失分片 2 必须上传");
            _grpcClient.Verify(
                c => c.UploadChunkAsync(
                    It.Is<UploadChunkRequest>(r => r.ChunkIndex == 3),
                    It.IsAny<CallOptions>()),
                Times.Once, "缺失分片 3 必须上传");
        });

        // 进度反馈：每上传一个分片推送一次（共 2 次，计数 3 → 4）
        Assert.That(progressReports, Has.Count.EqualTo(2));
        Assert.That(progressReports[0].UploadedChunks, Is.EqualTo(3));
        Assert.That(progressReports[0].CurrentChunkIndex, Is.EqualTo(2));
        Assert.That(progressReports[1].UploadedChunks, Is.EqualTo(4));
        Assert.That(progressReports[1].Percent, Is.EqualTo(100));
    }

    [Test]
    public async Task ResumeChunkUploadAsync_分片上传失败应中止并返回错误()
    {
        _grpcClient
            .Setup(c => c.GetChunkStatusAsync(It.IsAny<GetChunkStatusRequest>(), It.IsAny<CallOptions>()))
            .Returns(GrpcTestHelper.Success(new GetChunkStatusResponse
            {
                Success = true,
                FileKey = FileKey,
                TotalChunks = 2,
                Status = ChunkUploadStatus.ChunkUploading
            }));
        _grpcClient
            .Setup(c => c.UploadChunkAsync(It.IsAny<UploadChunkRequest>(), It.IsAny<CallOptions>()))
            .Returns(GrpcTestHelper.Throws<UploadChunkResponse>(
                GrpcTestHelper.RpcError(StatusCode.DataLoss, "分片损坏")));

        var chunks = new Dictionary<int, byte[]> { [0] = new byte[] { 1 } };

        var result = await _client.ResumeChunkUploadAsync(FileKey, 2, 1024, chunks);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("分片 0 上传失败"));
        });
    }

    // ─────────────────────────── 图片上传 ───────────────────────────

    [Test]
    public async Task UploadImageAsync_应映射图片尺寸与格式()
    {
        _grpcClient
            .Setup(c => c.UploadImageAsync(It.IsAny<UploadImageRequest>(), It.IsAny<CallOptions>()))
            .Returns(GrpcTestHelper.Success(new UploadImageResponse
            {
                Success = true,
                FileId = Guid.NewGuid().ToString(),
                FileMd5 = "img-md5",
                Width = 1920,
                Height = 1080,
                Format = "jpg"
            }));

        var result = await _client.UploadImageAsync(UserId, "photo.jpg", new byte[] { 1, 2 });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Width, Is.EqualTo(1920));
            Assert.That(result.Height, Is.EqualTo(1080));
            Assert.That(result.Format, Is.EqualTo("jpg"));
        });
    }

    /// <summary>
    /// 测试用 gRPC 客户端工厂：直接返回预设的 Mock 客户端。
    /// 生产代码通过 GrpcClientFactory（Aspire 服务发现）解析真实客户端。
    /// </summary>
    private sealed class StubGrpcClientFactory(Func<FileStorage.FileStorageClient> factory) : GrpcClientFactory
    {
        public override TClient CreateClient<TClient>(string name) where TClient : class
        {
            Assert.That(name, Is.EqualTo(FileStorageGrpcClient.ClientName),
                "客户端应通过服务发现名称 filedev-web-api 解析");
            return (TClient)(object)factory();
        }
    }
    // ─────────────────────────── 文件信息查询 / 删除 ───────────────────────────

        /// <summary>
    /// S-08 回归：HttpContext 存在 Bearer token 时，gRPC 调用元数据必须携带 Authorization 头
    /// （修复 NotMediator CreateScope 导致 ICurrentUserService 断链后，改为从 IHttpContextAccessor 取 token）。
    /// </summary>
    [Test]
    public async Task UploadImageAsync_HttpContext存在BearerToken_应附加Authorization元数据()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Authorization = "Bearer test-jwt-token";
        _httpContextAccessor.Setup(a => a.HttpContext).Returns(httpContext);

        CallOptions? captured = null;
        _grpcClient
            .Setup(c => c.UploadImageAsync(It.IsAny<UploadImageRequest>(), It.IsAny<CallOptions>()))
            .Callback<UploadImageRequest, CallOptions>((_, opts) => captured = opts)
            .Returns(GrpcTestHelper.Success(new UploadImageResponse
            {
                Success = true,
                FileId = Guid.NewGuid().ToString(),
                FileUri = "/files/x.jpg",
                FileMd5 = "md5",
                FileSize = 2
            }));

        await _client.UploadImageAsync(UserId, "photo.jpg", new byte[] { 1, 2 });

        Assert.That(captured, Is.Not.Null);
        var headers = captured!.Value.Headers;
        Assert.That(headers, Is.Not.Null, "gRPC 调用应携带请求元数据");
        // ⚠️ Grpc.Core.Metadata 将 Entry.Key 规范为小写（HTTP/2 头大小写不敏感），匹配用 OrdinalIgnoreCase
        var auth = headers!.SingleOrDefault(h => h.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase));
        Assert.That(auth, Is.Not.Null, "gRPC 调用元数据应包含 Authorization 头");
        Assert.That(auth!.Value, Is.EqualTo("Bearer test-jwt-token"));
    }

[Test]
    public async Task GetFileInfoAsync_成功响应应映射元数据()
    {
        var fileId = Guid.NewGuid();
        _grpcClient
            .Setup(c => c.GetFileInfoAsync(It.IsAny<GetFileInfoRequest>(), It.IsAny<CallOptions>()))
            .Returns(GrpcTestHelper.Success(new GetFileInfoResponse
            {
                Success = true,
                FileInfo = new FileInfo
                {
                    FileId = fileId.ToString(),
                    UserId = UserId.ToString(),
                    FileName = "a.txt",
                    FileSize = 1024,
                    FileUri = "files/a.txt",
                    FileMd5 = "md5",
                    FileType = FileType.FileDocument
                }
            }));

        var result = await _client.GetFileInfoAsync(fileId);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.FileId, Is.EqualTo(fileId));
            Assert.That(result.UserId, Is.EqualTo(UserId));
            Assert.That(result.FileName, Is.EqualTo("a.txt"));
            Assert.That(result.FileSize, Is.EqualTo(1024));
            Assert.That(result.FileType, Is.EqualTo("FileDocument"));
        });
    }

    [Test]
    public async Task GetFileInfoAsync_文件不存在应返回失败()
    {
        _grpcClient
            .Setup(c => c.GetFileInfoAsync(It.IsAny<GetFileInfoRequest>(), It.IsAny<CallOptions>()))
            .Returns(GrpcTestHelper.Success(new GetFileInfoResponse
            {
                Success = false,
                ErrorMessage = "文件不存在"
            }));

        var result = await _client.GetFileInfoAsync(Guid.NewGuid());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Is.EqualTo("文件不存在"));
        });
    }

    [Test]
    public async Task DeleteFileAsync_成功应返回成功结果()
    {
        _grpcClient
            .Setup(c => c.DeleteFileAsync(It.IsAny<DeleteFileRequest>(), It.IsAny<CallOptions>()))
            .Returns(GrpcTestHelper.Success(new DeleteFileResponse { Success = true }));

        var result = await _client.DeleteFileAsync(Guid.NewGuid(), UserId);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            _grpcClient.Verify(c => c.DeleteFileAsync(
                It.Is<DeleteFileRequest>(r => r.UserId == UserId.ToString()), It.IsAny<CallOptions>()),
                Times.Once);
        });
    }

    // ─────────────────────────── 流式下载 ───────────────────────────

    [Test]
    public async Task DownloadFileAsync_应流式转发分片()
    {
        var fileId = Guid.NewGuid();
        var chunk1 = ByteString.CopyFrom(new byte[] { 1, 2, 3 });
        var chunk2 = ByteString.CopyFrom(new byte[] { 4, 5 });

        var reader = new Mock<IAsyncStreamReader<DownloadFileResponse>>();
        reader.SetupSequence(r => r.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(true))
            .Returns(Task.FromResult(true))
            .Returns(Task.FromResult(false));
        reader.SetupSequence(r => r.Current)
            .Returns(new DownloadFileResponse { ChunkData = chunk1 })
            .Returns(new DownloadFileResponse { ChunkData = chunk2 });

        var call = new AsyncServerStreamingCall<DownloadFileResponse>(
            reader.Object,
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => new Metadata(),
            () => { });

        _grpcClient
            .Setup(c => c.DownloadFile(It.IsAny<DownloadFileRequest>(), It.IsAny<CallOptions>()))
            .Returns(call);

        var result = await _client.DownloadFileAsync(fileId, UserId);

        Assert.That(result.Success, Is.True);

        var received = new List<byte[]>();
        await foreach (var chunk in result.Chunks)
        {
            received.Add(chunk);
        }

        Assert.Multiple(() =>
        {
            Assert.That(received.Count, Is.EqualTo(2));
            Assert.That(received[0], Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(received[1], Is.EqualTo(new byte[] { 4, 5 }));
            _grpcClient.Verify(c => c.DownloadFile(
                It.Is<DownloadFileRequest>(r => r.FileId == fileId.ToString() && r.UserId == UserId.ToString()),
                It.IsAny<CallOptions>()), Times.Once);
        });
    }

    /// <summary>
    /// 回归：gRPC 下载错误（文件不存在/权限拒绝/认证失败）必须在方法返回前暴露——
    /// 惰性流错误若延迟到 HTTP 200 后的流式写入阶段，客户端收到的是中断响应而非明确错误。
    /// </summary>
    [Test]
    public async Task DownloadFileAsync_gRPC错误_应返回失败结果并映射错误信息()
    {
        var fileId = Guid.NewGuid();

        var reader = new Mock<IAsyncStreamReader<DownloadFileResponse>>();
        reader.Setup(r => r.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(Task.FromException<bool>(GrpcTestHelper.RpcError(StatusCode.NotFound, "文件不存在")));

        var call = new AsyncServerStreamingCall<DownloadFileResponse>(
            reader.Object,
            Task.FromResult(new Metadata()),
            () => new Status(StatusCode.NotFound, "文件不存在"),
            () => new Metadata(),
            () => { });

        _grpcClient
            .Setup(c => c.DownloadFile(It.IsAny<DownloadFileRequest>(), It.IsAny<CallOptions>()))
            .Returns(call);

        var result = await _client.DownloadFileAsync(fileId, UserId);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False, "gRPC 错误应前移为失败结果而非惰性流错误");
            Assert.That(result.ErrorMessage, Does.Contain("文件不存在"));
        });

        // 失败结果不应提供可枚举分片（空流占位）
        var received = new List<byte[]>();
        await foreach (var chunk in result.Chunks)
        {
            received.Add(chunk);
        }
        Assert.That(received, Is.Empty);
    }

    [Test]
    public async Task DownloadImageAsync_应透传缩放参数并流式转发()
    {
        var fileId = Guid.NewGuid();
        var chunk = ByteString.CopyFrom(new byte[] { 9, 8, 7 });

        var reader = new Mock<IAsyncStreamReader<DownloadImageResponse>>();
        reader.SetupSequence(r => r.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(true))
            .Returns(Task.FromResult(false));
        reader.Setup(r => r.Current)
            .Returns(new DownloadImageResponse { ChunkData = chunk });

        var call = new AsyncServerStreamingCall<DownloadImageResponse>(
            reader.Object,
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => new Metadata(),
            () => { });

        _grpcClient
            .Setup(c => c.DownloadImage(It.IsAny<DownloadImageRequest>(), It.IsAny<CallOptions>()))
            .Returns(call);

        var result = await _client.DownloadImageAsync(fileId, UserId, 320, 240);

        Assert.That(result.Success, Is.True);

        var received = new List<byte[]>();
        await foreach (var c in result.Chunks)
        {
            received.Add(c);
        }

        Assert.Multiple(() =>
        {
            Assert.That(received, Has.Count.EqualTo(1));
            Assert.That(received[0], Is.EqualTo(new byte[] { 9, 8, 7 }));
            _grpcClient.Verify(c => c.DownloadImage(
                It.Is<DownloadImageRequest>(r => r.FileId == fileId.ToString()
                    && r.ResizeWidth == 320 && r.ResizeHeight == 240),
                It.IsAny<CallOptions>()), Times.Once);
        });
    }

}
