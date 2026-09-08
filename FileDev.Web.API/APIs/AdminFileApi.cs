using Commons.Result;
using FileDev.Domain.Entities;
using FileDev.Domain.IRepository;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Mvc;

namespace FileDev.Web.API.APIs;

/// <summary>
/// 管理端文件 API（仅 Root / Administrator 可调用，挂载于 /api/filestorage/admin 鉴权分组）。
/// <para>
/// 安全约定：
/// - 列表仅返回投影字段（不含物理路径等敏感存储细节）；
/// - 删除 = 软删除（SoftDelete 触发 DeleteFileEvent 级联物理清理），并同步释放用户存储额度；
/// - 管理端操作必须为管理员，非管理员一律 403（与 Identity "AdminOnly" 角色语义一致）。
/// </para>
/// </summary>
public static class AdminFileApi
{
    public static RouteGroupBuilder MapAdminFileApi(this RouteGroupBuilder routeGroupBuilder)
    {
        var route = routeGroupBuilder.MapGroup("/admin")
            .WithHttpLogging(HttpLoggingFields.RequestPath | HttpLoggingFields.ResponseStatusCode);

        // GET /api/filestorage/admin/files?page&pageSize&keyword&userId — 文件列表（管理员）
        route.MapGet("/files", ListFilesAsync)
            .WithSummary("文件列表（管理员）")
            .WithDescription("分页查询全量文件，支持按文件名关键字/所属用户过滤，仅返回活跃文件");

        // DELETE /api/filestorage/admin/files/{fileId} — 删除文件（管理员）
        route.MapDelete("/files/{fileId:guid}", DeleteFileAsync)
            .WithSummary("删除文件（管理员）")
            .WithDescription("软删除指定文件并级联清理物理数据、释放额度");

        return route;
    }

    /// <summary>管理员校验：Role claim 含 Root / Administrator（与 Identity "AdminOnly" 策略一致）</summary>
    private static bool IsAdmin(HttpContext context)
    {
        return context.User
            .FindAll(System.Security.Claims.ClaimTypes.Role)
            .SelectMany(c => c.Value.Split(',',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Any(r => r is "Root" or "Administrator");
    }

    private static async Task<IResult> ListFilesAsync(
        HttpContext context,
        [FromServices] INotFileRepository notFileRepository,
        [FromQuery] string? keyword,
        [FromQuery] Guid? userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (!IsAdmin(context))
            return Results.Json(ApiResponseResult.Failure("仅管理员可查看文件列表", 403), statusCode: 403);

        try
        {
            var files = await notFileRepository.GetAllFilesAsync();

            var list = (files ?? [])
                .Where(f => !f.IsDeleted)
                .Where(f => string.IsNullOrWhiteSpace(keyword)
                            || f.FileName.Contains(keyword.Trim(), StringComparison.OrdinalIgnoreCase))
                .Where(f => !userId.HasValue || f.UserId == userId.Value)
                .OrderByDescending(f => f.UploadTime)
                .ToList();

            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 100) pageSize = 100;

            var items = list
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(f => new
                {
                    f.FileId,
                    f.UserId,
                    f.FileName,
                    f.FileDescription,
                    f.FileSize,
                    FileUri = f.FileUri.ToString(),
                    IsPublic = f.IsPublic,
                    Source = f.Source.ToString(),
                    UploadTime = f.UploadTime
                })
                .ToList();

            return Results.Ok(new { ok = true, data = items, totalCount = list.Count, page, pageSize });
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponseResult.Failure($"获取文件列表失败: {ex.Message}", 500), statusCode: 500);
        }
    }

    private static async Task<IResult> DeleteFileAsync(
        HttpContext context,
        Guid fileId,
        [FromServices] INotFileRepository notFileRepository,
        [FromServices] IUserFileInfoRepository userFileInfoRepository,
        CancellationToken ct = default)
    {
        if (!IsAdmin(context))
            return Results.Json(ApiResponseResult.Failure("仅管理员可删除文件", 403), statusCode: 403);

        try
        {
            var file = await notFileRepository.GetFileByIdAsync(fileId);
            if (file is null || file.IsDeleted)
                return Results.NotFound(ApiResponseResult.NotFound("文件不存在"));

            // 软删（SoftDelete 触发 DeleteFileEvent，级联物理清理）
            file.SoftDelete();
            await notFileRepository.UpdateFileAsync(file);

            // 释放用户存储额度（与 NotFileService.DeleteFileAsync 同事务语义）
            await userFileInfoRepository.ReleaseAsync(file.UserId, file.FileSize, ct);

            await notFileRepository.UnitOfWork.SaveEntitiesAsync(ct);

            return Results.Ok(new { ok = true, message = "文件已删除" });
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponseResult.Failure($"删除文件失败: {ex.Message}", 500), statusCode: 500);
        }
    }
}
