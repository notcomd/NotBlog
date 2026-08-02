using Message.Web.API.Application.Commands.Files;
using Message.Web.API.Application.Queries.Files;

namespace Message.Web.API.APIs;

/// <summary>
/// 文件接口（静态函数模式 + CQRS）。
/// <para>
/// 设计约定：
/// - 所有端点处理程序均为<b>静态函数</b>（不使用 Action/Lambda 创建接口）；
/// - 依赖服务通过 <c>[FromServices]</c> 特性注入，生命周期由服务注册文件统一管理；
/// - 数据写操作通过 <see cref="INotMediator"/> 分发到命令处理程序（Commands），
///   命令仅返回操作结果（bool / 新实体 ID / 分片操作结果对象），不返回业务实体/DTO；
/// - 数据读操作通过 <see cref="INotMediator"/> 分发到查询处理程序（Queries），
///   查询不修改任何数据状态，仅返回只读结果；
/// - 分片上传（断点续传）统一走 gRPC 文件服务（FileDev.Web.API），
///   提供 REST 与 SignalR（见 MessageHub）双通道；
/// - 路由参数（如 {id}、{messageId}）由框架按名称绑定，请求体使用 <c>[FromBody]</c>。
/// </para>
/// </summary>
public static class FilesApi
{
    /// <summary>映射文件相关端点组</summary>
    public static RouteGroupBuilder MapFilesApi(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/files")
            .WithTags("Files")
            .RequireAuthorization();

        // 1. POST / — 上传文件
        group.MapPost("/", UploadFileAsync)
            .WithSummary("上传文件")
            .WithDescription("上传文件附件")
            .Produces<ApiResponse<Guid>>()
            .Accepts<UploadFileRequest>("application/json");

        // 2. GET /{id} — 获取文件
        group.MapGet("/{id}", GetFileAsync)
            .WithSummary("获取文件")
            .WithDescription("根据文件ID获取文件信息")
            .Produces<ApiResponse<FileAttachmentDto>>();

        // 3. GET /{id}/download — 下载文件
        group.MapGet("/{id}/download", DownloadFileAsync)
            .WithSummary("下载文件")
            .WithDescription("下载文件并记录下载次数，重定向至文件URL")
            .Produces(StatusCodes.Status302Found);

        // 4. GET /{id}/preview — 预览文件
        group.MapGet("/{id}/preview", PreviewFileAsync)
            .WithSummary("预览文件")
            .WithDescription("获取文件预览信息，仅支持图片类型")
            .Produces<ApiResponse<FileAttachmentDto>>();

        // 5. DELETE /{id} — 删除文件
        group.MapDelete("/{id}", DeleteFileAsync)
            .WithSummary("删除文件")
            .WithDescription("删除指定文件")
            .Produces<ApiResponse>();

        // 6. GET /message/{messageId} — 获取消息附件
        group.MapGet("/message/{messageId}", GetMessageFilesAsync)
            .WithSummary("获取消息附件")
            .WithDescription("根据消息ID获取所有附件文件")
            .Produces<ApiResponse<IEnumerable<FileAttachmentDto>>>();

        // 7. GET /{id}/exists — 检查文件是否存在
        group.MapGet("/{id}/exists", FileExistsAsync)
            .WithSummary("检查文件是否存在")
            .WithDescription("检查指定文件是否存在")
            .Produces<ApiResponse<bool>>();

        // 8. GET /{id}/download-count — 获取下载次数
        group.MapGet("/{id}/download-count", GetDownloadCountAsync)
            .WithSummary("获取下载次数")
            .WithDescription("获取指定文件的下载次数")
            .Produces<ApiResponse<int>>();

        // 9. GET /{id}/size — 获取文件大小
        group.MapGet("/{id}/size", GetFileSizeAsync)
            .WithSummary("获取文件大小")
            .WithDescription("获取指定文件的格式化大小")
            .Produces<ApiResponse<string>>();

        // 10. GET /{id}/type — 获取文件类型信息
        group.MapGet("/{id}/type", GetFileTypeAsync)
            .WithSummary("获取文件类型信息")
            .WithDescription("获取指定文件的类型分类信息（图片/视频/音频/文档）")
            .Produces<ApiResponse<FileTypeInfo>>();

        // ── 大文件分片上传（断点续传，REST 通道）──

        // 11. POST /chunk/init — 初始化分片上传
        group.MapPost("/chunk/init", InitChunkUploadAsync)
            .WithSummary("初始化分片上传")
            .WithDescription("初始化大文件分片上传，返回 fileKey 与已上传分片（断点续传基础）")
            .Produces<ApiResponse<ChunkUploadInitResult>>();

        // 12. POST /chunk/upload — 上传单个分片
        group.MapPost("/chunk/upload", UploadChunkAsync)
            .WithSummary("上传分片")
            .WithDescription("上传指定索引的文件分片")
            .Produces<ApiResponse<ChunkUploadResult>>();

        // 13. POST /chunk/status — 查询分片上传状态
        group.MapPost("/chunk/status", GetChunkStatusAsync)
            .WithSummary("查询分片上传状态")
            .WithDescription("查询已上传的分片索引，用于断点续传决策")
            .Produces<ApiResponse<ChunkStatusResult>>();

        // 14. POST /chunk/merge — 合并分片
        group.MapPost("/chunk/merge", MergeChunksAsync)
            .WithSummary("合并分片")
            .WithDescription("合并所有分片生成最终文件")
            .Produces<ApiResponse<MergeChunksResult>>();

        // 15. POST /chunk/cancel — 取消分片上传
        group.MapPost("/chunk/cancel", CancelChunkUploadAsync)
            .WithSummary("取消分片上传")
            .WithDescription("取消分片上传并清理服务端临时数据")
            .Produces<ApiResponse<CancelChunkUploadResult>>();

        // 16. POST /chunk/resume — 断点续传
        group.MapPost("/chunk/resume", ResumeChunkUploadAsync)
            .WithSummary("断点续传")
            .WithDescription("一次性提交缺失分片，服务端自动跳过已上传分片完成续传")
            .Produces<ApiResponse<ChunkStatusResult>>();

        return group;
    }

    /// <summary>
    /// 上传文件（记录文件附件元数据）。
    /// 命令侧（UploadFileCommand）：仅返回新附件 ID。
    /// </summary>
    /// <param name="request">上传文件请求体</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>新附件 ID</returns>
    private static async Task<IResult> UploadFileAsync(
        [FromBody] UploadFileRequest request,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var attachmentId = await mediator.SendAsync(
                new UploadFileCommand(
                    request.MessageId,
                    request.FileName,
                    request.FileType,
                    request.FileSize,
                    new Uri(request.FileUrl)),
                ct);

            return Results.Ok(ApiResponse<Guid>.Created(attachmentId, "文件上传成功"));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse<Guid>.Error($"文件上传失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>
    /// 获取文件信息（查询侧）。
    /// </summary>
    /// <param name="id">文件ID（路由参数）</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>文件附件 DTO</returns>
    private static async Task<IResult> GetFileAsync(
        Guid id,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var file = await mediator.SendAsync(new GetFileQuery(id), ct);
            if (file == null)
                return Results.Json(ApiResponse<FileAttachmentDto>.NotFound("文件不存在"), statusCode: 404);

            return Results.Ok(ApiResponse<FileAttachmentDto>.Ok(MapToDto(file)));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse<FileAttachmentDto>.Error($"获取文件失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>
    /// 下载文件（记录下载次数并重定向至文件 URL）。
    /// 先经查询侧获取文件（不存在则 404），再经命令侧记录下载次数。
    /// </summary>
    /// <param name="id">文件ID（路由参数）</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>重定向响应</returns>
    private static async Task<IResult> DownloadFileAsync(
        Guid id,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var file = await mediator.SendAsync(new GetFileQuery(id), ct);
            if (file == null)
                return Results.Json(ApiResponse.NotFound("文件不存在"), statusCode: 404);

            await mediator.SendAsync(new RecordDownloadCommand(id), ct);

            return Results.Redirect(file.FileUri.ToString());
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse.Error($"下载文件失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>
    /// 预览文件（仅支持图片类型）。
    /// 经查询侧获取文件类型分类信息，非图片类型返回 400。
    /// </summary>
    /// <param name="id">文件ID（路由参数）</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>文件预览信息</returns>
    private static async Task<IResult> PreviewFileAsync(
        Guid id,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var result = await mediator.SendAsync(new GetFileTypeQuery(id), ct);
            if (result.File == null)
                return Results.Json(ApiResponse<FileAttachmentDto>.NotFound("文件不存在"), statusCode: 404);

            if (!result.IsImage)
                return Results.Json(ApiResponse<FileAttachmentDto>.BadRequest("该文件不支持预览"), statusCode: 400);

            return Results.Ok(ApiResponse<FileAttachmentDto>.Ok(MapToDto(result.File)));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse<FileAttachmentDto>.Error($"预览文件失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>
    /// 删除文件（命令侧）。
    /// </summary>
    /// <param name="id">文件ID（路由参数）</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>操作结果</returns>
    private static async Task<IResult> DeleteFileAsync(
        Guid id,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            await mediator.SendAsync(new DeleteFileCommand(id), ct);
            return Results.Ok(ApiResponse.Ok("文件已删除"));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse.Error($"删除文件失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>
    /// 获取消息的全部附件（查询侧）。
    /// </summary>
    /// <param name="messageId">消息ID（路由参数）</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>文件附件 DTO 列表</returns>
    private static async Task<IResult> GetMessageFilesAsync(
        Guid messageId,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var files = await mediator.SendAsync(new GetMessageFilesQuery(messageId), ct);
            return Results.Ok(ApiResponse<IEnumerable<FileAttachmentDto>>.Ok(files.Select(MapToDto)));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse<IEnumerable<FileAttachmentDto>>.Error($"获取消息附件失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>
    /// 检查文件是否存在（查询侧）。
    /// </summary>
    /// <param name="id">文件ID（路由参数）</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>是否存在</returns>
    private static async Task<IResult> FileExistsAsync(
        Guid id,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var exists = await mediator.SendAsync(new FileExistsQuery(id), ct);
            return Results.Ok(ApiResponse<bool>.Ok(exists));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse<bool>.Error($"检查文件是否存在失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>
    /// 获取文件下载次数（查询侧）。
    /// </summary>
    /// <param name="id">文件ID（路由参数）</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>下载次数</returns>
    private static async Task<IResult> GetDownloadCountAsync(
        Guid id,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var count = await mediator.SendAsync(new GetDownloadCountQuery(id), ct);
            return Results.Ok(ApiResponse<int>.Ok(count));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse<int>.Error($"获取下载次数失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>
    /// 获取文件格式化大小（查询侧）。
    /// </summary>
    /// <param name="id">文件ID（路由参数）</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>格式化后的文件大小</returns>
    private static async Task<IResult> GetFileSizeAsync(
        Guid id,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var formattedSize = await mediator.SendAsync(new GetFileSizeQuery(id), ct);
            if (formattedSize == null)
                return Results.Json(ApiResponse<string>.NotFound("文件不存在"), statusCode: 404);

            return Results.Ok(ApiResponse<string>.Ok(formattedSize));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse<string>.Error($"获取文件大小失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>
    /// 获取文件类型分类信息（查询侧）。
    /// </summary>
    /// <param name="id">文件ID（路由参数）</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>文件类型分类信息</returns>
    private static async Task<IResult> GetFileTypeAsync(
        Guid id,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var result = await mediator.SendAsync(new GetFileTypeQuery(id), ct);
            if (result.File == null)
                return Results.Json(ApiResponse<FileTypeInfo>.NotFound("文件不存在"), statusCode: 404);

            var typeInfo = new FileTypeInfo
            {
                FileType = result.File.FileType,
                IsImage = result.IsImage,
                IsVideo = result.IsVideo,
                IsAudio = result.IsAudio,
                IsDocument = result.IsDocument
            };

            return Results.Ok(ApiResponse<FileTypeInfo>.Ok(typeInfo));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse<FileTypeInfo>.Error($"获取文件类型失败: {ex.Message}"), statusCode: 500);
        }
    }

    // ─────────────────────────────────────────────────────
    // 大文件分片上传（断点续传，REST 通道）
    // 内部统一调用 FileDev.Web.API 的 gRPC 文件服务
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// 初始化分片上传，返回 fileKey 与分片参数；若存在已上传分片则一并返回（断点续传基础）。
    /// 命令侧（InitChunkUploadCommand）。
    /// </summary>
    /// <param name="request">初始化分片上传请求体</param>
    /// <param name="currentUser">当前用户服务</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>分片上传初始化结果</returns>
    private static async Task<IResult> InitChunkUploadAsync(
        [FromBody] ChunkUploadInitRequest request,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var userId = currentUser.GetUserId();
            var result = await mediator.SendAsync(
                new InitChunkUploadCommand(
                    userId,
                    request.FileName,
                    request.TotalSize,
                    request.FileMd5,
                    request.Description,
                    request.IsPublic),
                ct);

            return result.Success
                ? Results.Ok(ApiResponse<ChunkUploadInitResult>.Ok(result, "分片上传初始化成功"))
                : Results.BadRequest(ApiResponse<ChunkUploadInitResult>.Error(result.ErrorMessage ?? "初始化分片上传失败"));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse<ChunkUploadInitResult>.Error($"初始化分片上传失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>
    /// 上传单个分片。
    /// 命令侧（UploadChunkCommand）。
    /// </summary>
    /// <param name="request">分片上传请求体（fileKey + 分片索引 + 分片数据）</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>分片上传结果</returns>
    private static async Task<IResult> UploadChunkAsync(
        [FromBody] ChunkUploadRequest request,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var result = await mediator.SendAsync(
                new UploadChunkCommand(
                    request.FileKey,
                    request.ChunkIndex,
                    request.ChunkData,
                    request.ChunkMd5),
                ct);

            return result.Success
                ? Results.Ok(ApiResponse<ChunkUploadResult>.Ok(result, "分片上传成功"))
                : Results.BadRequest(ApiResponse<ChunkUploadResult>.Error(result.ErrorMessage ?? "上传分片失败"));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse<ChunkUploadResult>.Error($"上传分片失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>
    /// 查询分片上传状态（已上传分片索引），供客户端决定续传哪些分片。
    /// 查询侧（GetChunkStatusQuery）。
    /// </summary>
    /// <param name="request">状态查询请求体（fileKey）</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>分片上传状态</returns>
    private static async Task<IResult> GetChunkStatusAsync(
        [FromBody] ChunkStatusRequest request,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var result = await mediator.SendAsync(new GetChunkStatusQuery(request.FileKey), ct);

            return result.Success
                ? Results.Ok(ApiResponse<ChunkStatusResult>.Ok(result, "状态查询成功"))
                : Results.BadRequest(ApiResponse<ChunkStatusResult>.Error(result.ErrorMessage ?? "查询分片状态失败"));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse<ChunkStatusResult>.Error($"查询分片状态失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>
    /// 合并分片，生成最终文件并返回文件元数据。
    /// 命令侧（MergeChunksCommand）。
    /// </summary>
    /// <param name="request">合并分片请求体（fileKey + 可选文件名/描述）</param>
    /// <param name="currentUser">当前用户服务</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>合并结果（含最终文件信息）</returns>
    private static async Task<IResult> MergeChunksAsync(
        [FromBody] ChunkMergeRequest request,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var userId = currentUser.GetUserId();
            var result = await mediator.SendAsync(
                new MergeChunksCommand(
                    request.FileKey,
                    userId,
                    request.FileName,
                    request.Description),
                ct);

            return result.Success
                ? Results.Ok(ApiResponse<MergeChunksResult>.Ok(result, "分片合并成功"))
                : Results.BadRequest(ApiResponse<MergeChunksResult>.Error(result.ErrorMessage ?? "合并分片失败"));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse<MergeChunksResult>.Error($"合并分片失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>
    /// 取消分片上传，清理服务端临时数据。
    /// 命令侧（CancelChunkUploadCommand）。
    /// </summary>
    /// <param name="request">取消分片上传请求体（fileKey）</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>取消结果</returns>
    private static async Task<IResult> CancelChunkUploadAsync(
        [FromBody] ChunkCancelRequest request,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var result = await mediator.SendAsync(new CancelChunkUploadCommand(request.FileKey), ct);

            return result.Success
                ? Results.Ok(ApiResponse<CancelChunkUploadResult>.Ok(result, "分片上传已取消"))
                : Results.BadRequest(ApiResponse<CancelChunkUploadResult>.Error(result.ErrorMessage ?? "取消分片上传失败"));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse<CancelChunkUploadResult>.Error($"取消分片上传失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>
    /// 断点续传：一次性提交缺失分片集合，服务端查询已上传分片后仅上传缺失部分。
    /// 命令侧（ResumeChunkUploadCommand）。
    /// </summary>
    /// <param name="request">断点续传请求体（fileKey + 总分片数 + 缺失分片集合）</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>续传完成后的分片状态</returns>
    private static async Task<IResult> ResumeChunkUploadAsync(
        [FromBody] ChunkResumeRequest request,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var result = await mediator.SendAsync(
                new ResumeChunkUploadCommand(
                    request.FileKey,
                    request.TotalChunks,
                    request.ChunkSize,
                    request.Chunks),
                ct);

            return result.Success
                ? Results.Ok(ApiResponse<ChunkStatusResult>.Ok(result, "断点续传完成"))
                : Results.BadRequest(ApiResponse<ChunkStatusResult>.Error(result.ErrorMessage ?? "断点续传失败"));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse<ChunkStatusResult>.Error($"断点续传失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>文件附件实体 → DTO 映射</summary>
    private static FileAttachmentDto MapToDto(FileAttachment file) => new()
    {
        AttachmentId = file.AttachmentId,
        FileName = file.FileName,
        FileType = file.FileType,
        FileSize = file.FileSize,
        FileUrl = file.FileUri.ToString(),
        ThumbnailUrl = file.ThumbnailUri?.ToString(),
        UploadTime = file.UploadTime,
        DownloadCount = file.DownloadCount
    };

    /// <summary>文件类型分类信息</summary>
    public class FileTypeInfo
    {
        public string FileType { get; init; } = string.Empty;
        public bool IsImage { get; init; }
        public bool IsVideo { get; init; }
        public bool IsAudio { get; init; }
        public bool IsDocument { get; init; }
    }
}
