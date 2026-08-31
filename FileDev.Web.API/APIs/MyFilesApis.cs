using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace FileDev.Web.API.APIs;

/// <summary>
/// 我的文件列表接口（/api/filestorage/files/my）：
/// 返回当前用户已上传且未删除的文件（按上传时间倒序）。
/// 响应结构：{ ok, items: [{ fileId, name, url, type, size, createdAt }] }
/// </summary>
public static class MyFilesApis
{
    private const string ImageExts = ".jpg.jpeg.png.gif.bmp.webp.svg.ico";
    private const string VideoExts = ".mp4.avi.mkv.mov.wmv.flv.webm.m4v";

    public static RouteGroupBuilder MapMyFilesApis(this RouteGroupBuilder routeGroupBuilder)
    {
        var router = routeGroupBuilder.MapGroup("/files");

        router.MapGet("/my", GetMyFilesAsync)
            .WithName("MyFiles")
            .WithDescription("获取当前用户已上传文件列表（按上传时间倒序；分页）");

        return router;
    }

    private static async Task<IResult> GetMyFilesAsync(
        [FromServices] HttpContext context,
        [FromServices] FileServicesDi fileServicesDi,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var userId = FileApiHelpers.GetUserId(context);
        if (userId == null)
            return Results.Json(new { ok = false, error = "未认证" }, statusCode: 401);

        try
        {
            var files = (await fileServicesDi.NotFileService.GetFilesByUserIdAsync(userId.Value))
                .Where(f => !f.IsDeleted)
                .OrderByDescending(f => f.UploadTime);

            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 100) pageSize = 100;

            var total = files.Count();
            var items = files
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(f => new
                {
                    fileId = f.FileId,
                    name = f.FileName,
                    url = f.FileUri,
                    type = ResolveType(f.FileName),
                    size = f.FileSize,
                    createdAt = f.UploadTime
                });

            return Results.Ok(new { ok = true, items, total, page, pageSize });
        }
        catch (Exception ex)
        {
            fileServicesDi.Logger.LogError(ex, "获取我的文件列表失败: {UserId}", userId.Value);
            return Results.Json(new { ok = false, error = "获取文件列表失败" }, statusCode: 500);
        }
    }

    // 由扩展名推断前端展示类型（image/video/doc）
    private static string ResolveType(string fileName)
    {
        var ext = System.IO.Path.GetExtension(fileName).ToLowerInvariant().TrimStart('.');
        if (ImageExts.Contains('.' + ext)) return "image";
        if (VideoExts.Contains('.' + ext)) return "video";
        return "doc";
    }
}