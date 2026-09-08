using Commons.Result;
using FileDev.Web.API.Dto;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Mvc;
namespace FileDev.Web.API.APIs;

/// <summary>
/// 标签 API — 替代原"文件组"管理（类文件组管理）。
/// 支持自定义/改名/删除/查询标签，以及文件与标签的多对多归属关系操作。
/// </summary>
public static class FileTagApi
{


    public static RouteGroupBuilder FileTagApis(this RouteGroupBuilder routeGroupBuilder)
    {
        var route = routeGroupBuilder.MapGroup("/filestorage/tags")
            .RequireAuthorization()
            .RequireResourcePermissions("api:file");

        route.MapPost("/", CreateTagAsync)
            .WithHttpLogging(HttpLoggingFields.RequestPath | HttpLoggingFields.ResponseStatusCode, 1, 1);
        route.MapGet("/", ListTagsAsync)
            .WithHttpLogging(HttpLoggingFields.RequestPath | HttpLoggingFields.ResponseStatusCode, 1, 1);
        route.MapPut("/{tagId:guid}", RenameTagAsync)
            .WithHttpLogging(HttpLoggingFields.RequestPath | HttpLoggingFields.ResponseStatusCode, 1, 1);
        route.MapDelete("/{tagId:guid}", DeleteTagAsync)
            .WithHttpLogging(HttpLoggingFields.RequestPath | HttpLoggingFields.ResponseStatusCode, 1, 1);
        route.MapPost("/{tagId:guid}/files/{fileId:guid}", AddFileToTagAsync)
            .WithHttpLogging(HttpLoggingFields.RequestPath | HttpLoggingFields.ResponseStatusCode, 1, 1);
        route.MapDelete("/{tagId:guid}/files/{fileId:guid}", RemoveFileFromTagAsync)
            .WithHttpLogging(HttpLoggingFields.RequestPath | HttpLoggingFields.ResponseStatusCode, 1, 1);
        route.MapGet("/{tagId:guid}/files", ListTagFilesAsync)
            .WithHttpLogging(HttpLoggingFields.RequestPath | HttpLoggingFields.ResponseStatusCode, 1, 1);

        return route;
    }

    private static async Task<IResult> CreateTagAsync(
        HttpContext httpContext,
        [FromServices] FileServicesDi servicesDi,
        [FromServices] ILoggerFactory loggerFactory,
        [FromBody] CreateTagRequest body,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("FileTagApi");
        var userGuid = FileApiHelpers.GetUserId(httpContext);
        if (userGuid == null)
            return Results.Json(ApiResponseResult.Failure("未认证", 401), statusCode: 401);

        try
        {
            if (body == null || string.IsNullOrWhiteSpace(body.Name))
                return Results.Json(ApiResponseResult.Failure("标签名称不能为空", 400), statusCode: 400);

            var command = new CreateTagCommand(userGuid.Value, body.Name.Trim(), body.Description);
            var identified = new IdentifiedCommand<CreateTagCommand, bool>(
                FileApiHelpers.GetIdempotencyKey(httpContext), command);
            await servicesDi.NotMediator.SendAsync(identified, cancellationToken);
            return Results.Json(new { ok = true });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "创建标签失败: UserId={UserId}, Name={Name}", userGuid.Value, body?.Name);
            return Results.Json(ApiResponseResult.Failure("请求处理失败", 500), statusCode: 500);
        }
    }

    private static async Task<IResult> ListTagsAsync(
        HttpContext httpContext,
        [FromServices] FileServicesDi servicesDi,
        CancellationToken cancellationToken)
    {
        var userGuid = FileApiHelpers.GetUserId(httpContext);
        if (userGuid == null)
            return Results.Json(ApiResponseResult.Failure("未认证", 401), statusCode: 401);

        var tags = await servicesDi.NotFileTagRepository.GetNotFileTagsByUserIdAsync(userGuid.Value);
        var dtos = tags.Select(t => new TagResponse(
            t.TagId, t.TagName, t.TagDescription, t.DefaultKind != FileDev.Domain.Enum.TagDefaultKind.None,
            t.FileIds, t.UploadTime, t.UpdateTime));
        return Results.Json(new { ok = true, data = dtos });
    }

    private static async Task<IResult> RenameTagAsync(
        HttpContext httpContext,
        [FromServices] FileServicesDi servicesDi,
        [FromServices] ILoggerFactory loggerFactory,
        Guid tagId,
        [FromBody] RenameTagRequest body,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("FileTagApi");
        var userGuid = FileApiHelpers.GetUserId(httpContext);
        if (userGuid == null)
            return Results.Json(ApiResponseResult.Failure("未认证", 401), statusCode: 401);

        try
        {
            if (body == null || string.IsNullOrWhiteSpace(body.NewName))
                return Results.Json(ApiResponseResult.Failure("标签名称不能为空", 400), statusCode: 400);

            var command = new RenameTagCommand(userGuid.Value, tagId, body.NewName.Trim());
            await servicesDi.NotMediator.SendAsync(command, cancellationToken);
            return Results.Json(new { ok = true });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "重命名标签失败: UserId={UserId}, TagId={TagId}", userGuid.Value, tagId);
            return Results.Json(ApiResponseResult.Failure("请求处理失败", 500), statusCode: 500);
        }
    }

    private static async Task<IResult> DeleteTagAsync(
        HttpContext httpContext,
        [FromServices] FileServicesDi servicesDi,
        [FromServices] ILoggerFactory loggerFactory,
        Guid tagId,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("FileTagApi");
        var userGuid = FileApiHelpers.GetUserId(httpContext);
        if (userGuid == null)
            return Results.Json(ApiResponseResult.Failure("未认证", 401), statusCode: 401);

        try
        {
            var command = new DeleteTagCommand(userGuid.Value, tagId);
            await servicesDi.NotMediator.SendAsync(command, cancellationToken);
            return Results.Json(new { ok = true });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "删除标签失败: UserId={UserId}, TagId={TagId}", userGuid.Value, tagId);
            return Results.Json(ApiResponseResult.Failure("请求处理失败", 500), statusCode: 500);
        }
    }

    private static async Task<IResult> AddFileToTagAsync(
        HttpContext httpContext,
        [FromServices] FileServicesDi servicesDi,
        [FromServices] ILoggerFactory loggerFactory,
        Guid tagId,
        Guid fileId,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("FileTagApi");
        var userGuid = FileApiHelpers.GetUserId(httpContext);
        if (userGuid == null)
            return Results.Json(ApiResponseResult.Failure("未认证", 401), statusCode: 401);

        try
        {
            var command = new TagAddFileCommand(userGuid.Value, tagId, fileId);
            var ok = await servicesDi.NotMediator.SendAsync(command, cancellationToken);
            return ok
                ? Results.Json(new { ok = true })
                : Results.Json(ApiResponseResult.Failure("文件不存在或已删除", 404), statusCode: 404);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "文件加入标签失败: UserId={UserId}, TagId={TagId}, FileId={FileId}", userGuid.Value, tagId, fileId);
            return Results.Json(ApiResponseResult.Failure("请求处理失败", 500), statusCode: 500);
        }
    }

    private static async Task<IResult> RemoveFileFromTagAsync(
        HttpContext httpContext,
        [FromServices] FileServicesDi servicesDi,
        [FromServices] ILoggerFactory loggerFactory,
        Guid tagId,
        Guid fileId,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("FileTagApi");
        var userGuid = FileApiHelpers.GetUserId(httpContext);
        if (userGuid == null)
            return Results.Json(ApiResponseResult.Failure("未认证", 401), statusCode: 401);

        try
        {
            var command = new TagRemoveFileCommand(userGuid.Value, tagId, fileId);
            await servicesDi.NotMediator.SendAsync(command, cancellationToken);
            return Results.Json(new { ok = true });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "从标签移除文件失败: UserId={UserId}, TagId={TagId}, FileId={FileId}", userGuid.Value, tagId, fileId);
            return Results.Json(ApiResponseResult.Failure("请求处理失败", 500), statusCode: 500);
        }
    }

    private static async Task<IResult> ListTagFilesAsync(
        [FromServices] HttpContext httpContext,
        [FromServices] FileServicesDi servicesDi,
        Guid tagId,
        CancellationToken cancellationToken)
    {
        var userGuid = FileApiHelpers.GetUserId(httpContext);
        if (userGuid == null)
            return Results.Json(ApiResponseResult.Failure("未认证", 401), statusCode: 401);

        // 读取标签（校验归属），按 FileIds 批量查询文件
        var tag = await servicesDi.NotFileTagRepository.GetNotFileTagByIdAsync(tagId);
        if (tag.UserId != userGuid.Value)
            return Results.Json(ApiResponseResult.Failure("无权访问他人标签", 403), statusCode: 403);

        var files = await servicesDi.NotFileRepository.GetFilesByIdsAsync(tag.FileIds);
        var dtos = files.Select(f => new TagFileResponse(
            f.FileId, f.FileName, f.FileSize, f.FileUri, f.FileMd5, f.FileTags, f.UploadTime));
        return Results.Json(new { ok = true, data = dtos });
    }
}
