using Message.Web.API.Application.Commands.Files;
using Message.Web.API.Application.Queries.Files;
using Message.Web.API.Extensions;

namespace Message.Web.API.APIs;

/// <summary>
/// 文件接口（静态函数模式 + CQRS）。
/// <para>
/// 设计约定（MessageApi-Redesign v2）：
/// - 所有端点处理程序均为<b>静态函数</b>（不使用 Action/Lambda 创建接口）；
/// - 依赖服务通过 <c>[FromServices]</c> 特性注入，生命周期由服务注册文件统一管理；
/// - 数据写操作通过 <see cref="INotMediator"/> 分发到命令处理程序（Commands），
///   命令仅返回操作结果（bool / 新实体 ID / 分片操作结果对象），不返回业务实体/DTO；
/// - 数据读操作通过 <see cref="INotMediator"/> 分发到查询处理程序（Queries），
///   查询不修改任何数据状态，仅返回只读结果；
/// - <b>文件上传唯一入口</b>：本组端点仅负责"上传"，全部经由 FileDev.Web.API 的 gRPC
///   文件服务完成（小文件 / 图片 / 分片断点续传），REST 与 SignalR（见 MessageHub）双通道
///   共享同一 <c>IFileStorageGrpcClient</c>；
/// - 上传响应统一返回 <see cref="FileRef"/>，客户端持 FileId 发送消息/推文时引用附件；
/// - 消息附件资源（列表/详情/下载/预览/删除）已迁移至 MessagesApi（/api/messages/attachments）。
/// </para>
/// </summary>
public static class FilesApi
{
    /// <summary>小文件上传大小上限（字节，10MB）；超限应改走分片上传通道</summary>
    private const long SmallFileSizeLimit = 10 * 1024 * 1024;

    /// <summary>映射文件上传端点组</summary>
    public static RouteGroupBuilder MapFilesApi(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/files")
            .WithTags("Files")
            .RequireAuthorization();

        // 1. POST /upload — 小文件上传（multipart/form-data，走 FileDev gRPC）
        group.MapPost("/upload", UploadSmallFileAsync)
            .WithSummary("上传小文件")
            .WithDescription("上传 ≤10MB 的小文件（multipart/form-data），内部经由 FileDev gRPC 存储，返回 FileRef")
            .DisableAntiforgery()
            .Produces<ApiResponse<FileRef>>()
            .Produces(StatusCodes.Status413PayloadTooLarge);

        // 2. POST /upload-image — 图片上传（multipart/form-data，走 FileDev gRPC）
        group.MapPost("/upload-image", UploadImageAsync)
            .WithSummary("上传图片")
            .WithDescription("上传图片（multipart/form-data），携带格式校验与尺寸解析，内部经由 FileDev gRPC 存储")
            .DisableAntiforgery()
            .Produces<ApiResponse<FileRef>>()
            .Produces(StatusCodes.Status413PayloadTooLarge);

        // ── 大文件分片上传（断点续传，REST 通道；内部统一走 FileDev gRPC）──

        // 3. POST /chunk/init — 初始化分片上传
        group.MapPost("/chunk/init", InitChunkUploadAsync)
            .WithSummary("初始化分片上传")
            .WithDescription("初始化大文件分片上传，返回 fileKey 与已上传分片（断点续传基础）")
            .Produces<ApiResponse<ChunkUploadInitResult>>();

        // 4. POST /chunk/upload — 上传单个分片（multipart/form-data）
        group.MapPost("/chunk/upload", UploadChunkAsync)
            .WithSummary("上传分片")
            .WithDescription("上传指定索引的文件分片（multipart/form-data：fileKey + chunkIndex + 分片文件）")
            .DisableAntiforgery()
            .Produces<ApiResponse<ChunkUploadResult>>();

        // 5. POST /chunk/status — 查询分片上传状态
        group.MapPost("/chunk/status", GetChunkStatusAsync)
            .WithSummary("查询分片上传状态")
            .WithDescription("查询已上传的分片索引，用于断点续传决策")
            .Produces<ApiResponse<ChunkStatusResult>>();

        // 6. POST /chunk/merge — 合并分片
        group.MapPost("/chunk/merge", MergeChunksAsync)
            .WithSummary("合并分片")
            .WithDescription("合并所有分片生成最终文件")
            .Produces<ApiResponse<MergeChunksResult>>();

        // 7. POST /chunk/cancel — 取消分片上传
        group.MapPost("/chunk/cancel", CancelChunkUploadAsync)
            .WithSummary("取消分片上传")
            .WithDescription("取消分片上传并清理服务端临时数据")
            .Produces<ApiResponse<CancelChunkUploadResult>>();

        // 8. POST /chunk/resume — 断点续传
        group.MapPost("/chunk/resume", ResumeChunkUploadAsync)
            .WithSummary("断点续传")
            .WithDescription("一次性提交缺失分片，服务端自动跳过已上传分片完成续传")
            .Produces<ApiResponse<ChunkStatusResult>>();

        return group;
    }

    /// <summary>
    /// 小文件上传（≤10MB）：multipart 接收 → FileDev gRPC 存储 → 返回 <see cref="FileRef"/>。
    /// </summary>
    /// <param name="file">文件（multipart 字段名：file）</param>
    /// <param name="description">文件描述（可选）</param>
    /// <param name="isPublic">是否公开文件（默认私有）</param>
    /// <param name="currentUser">当前用户服务</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>文件引用（FileRef）</returns>
    private static async Task<IResult> UploadSmallFileAsync(
        IFormFile file,
        [FromForm] string? description,
        [FromForm] bool isPublic,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return Results.Json(ApiResponse<FileRef>.BadRequest("文件内容不能为空"), statusCode: 400);

        if (file.Length > SmallFileSizeLimit)
            return Results.Json(
                ApiResponse<FileRef>.Error("文件超过10MB，请使用分片上传通道（/api/files/chunk/*）"),
                statusCode: StatusCodes.Status413PayloadTooLarge);

        try
        {
            var content = await ReadAllBytesAsync(file, ct);
            var result = await mediator.SendAsync(
                new UploadFileViaGrpcCommand(
                    currentUser.GetUserId(),
                    file.FileName,
                    content,
                    description,
                    isPublic),
                ct);

            if (!result.Success)
                return Results.BadRequest(ApiResponse<FileRef>.Error(result.ErrorMessage ?? "文件上传失败"));

            return Results.Ok(ApiResponse<FileRef>.Ok(
                new FileRef(
                    result.FileId!.Value,
                    result.FileUri!,
                    file.FileName,
                    result.FileSize,
                    result.FileMd5,
                    MimeTypeMap.FromFileName(file.FileName)),
                "文件上传成功"));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse<FileRef>.Error($"文件上传失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>
    /// 图片上传：multipart 接收 → FileDev gRPC（格式校验 + 尺寸解析）→ 返回 <see cref="FileRef"/>。
    /// </summary>
    /// <param name="file">图片文件（multipart 字段名：file）</param>
    /// <param name="description">文件描述（可选）</param>
    /// <param name="currentUser">当前用户服务</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>文件引用（FileRef，含宽高）</returns>
    private static async Task<IResult> UploadImageAsync(
        IFormFile file,
        [FromForm] string? description,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return Results.Json(ApiResponse<FileRef>.BadRequest("图片内容不能为空"), statusCode: 400);

        if (file.Length > SmallFileSizeLimit)
            return Results.Json(
                ApiResponse<FileRef>.Error("图片超过10MB，请使用分片上传通道（/api/files/chunk/*）"),
                statusCode: StatusCodes.Status413PayloadTooLarge);

        try
        {
            var content = await ReadAllBytesAsync(file, ct);
            var result = await mediator.SendAsync(
                new UploadImageViaGrpcCommand(
                    currentUser.GetUserId(),
                    file.FileName,
                    content,
                    description,
                    ValidateFormat: true),
                ct);

            if (!result.Success)
                return Results.BadRequest(ApiResponse<FileRef>.Error(result.ErrorMessage ?? "图片上传失败"));

            return Results.Ok(ApiResponse<FileRef>.Ok(
                new FileRef(
                    result.FileId!.Value,
                    result.FileUri!,
                    file.FileName,
                    result.FileSize,
                    result.FileMd5,
                    MimeTypeMap.FromFileName(file.FileName),
                    result.Width > 0 ? result.Width : null,
                    result.Height > 0 ? result.Height : null),
                "图片上传成功"));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse<FileRef>.Error($"图片上传失败: {ex.Message}"), statusCode: 500);
        }
    }

    /// <summary>
    /// 上传单个分片（multipart/form-data）。
    /// 命令侧（UploadChunkCommand），内部经 FileDev gRPC 上传。
    /// </summary>
    /// <param name="fileKey">分片上传记录键（表单字段）</param>
    /// <param name="chunkIndex">分片索引（表单字段，从0开始）</param>
    /// <param name="chunkMd5">分片 MD5（表单字段，可选）</param>
    /// <param name="file">分片二进制（表单字段名：file）</param>
    /// <param name="mediator">中介者（命令/查询分发）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>分片上传结果</returns>
    private static async Task<IResult> UploadChunkAsync(
        [FromForm] string fileKey,
        [FromForm] int chunkIndex,
        [FromForm] string? chunkMd5,
        IFormFile file,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return Results.Json(ApiResponse<ChunkUploadResult>.BadRequest("分片数据不能为空"), statusCode: 400);

        try
        {
            var chunkData = await ReadAllBytesAsync(file, ct);
            var result = await mediator.SendAsync(
                new UploadChunkCommand(fileKey, chunkIndex, chunkData, chunkMd5),
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

    /// <summary>读取 IFormFile 全部字节</summary>
    private static async Task<byte[]> ReadAllBytesAsync(IFormFile file, CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();
        using var ms = new MemoryStream((int)file.Length);
        await stream.CopyToAsync(ms, ct);
        return ms.ToArray();
    }
}
