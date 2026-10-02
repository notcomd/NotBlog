using Identity.Web.API.Application.Commands;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Mvc;
namespace Identity.Web.API.APIs;

public static class AvatarApi
{
    private const long MaxFileSize = 5 * 1024 * 1024; // 5MB

    public static RouteGroupBuilder MapAvatarApi(this RouteGroupBuilder routeBuilder)
    {
        var route = routeBuilder
            .MapGroup("/avatar")
            .WithTags("Avatar")
            .WithHttpLogging(HttpLoggingFields.All);

        route.MapPost("/upload", UploadAvatarAsync)
            .RequirePermission("api:identity:create")
            .WithName("UploadAvatar")
            .WithDescription("上传用户头像")
            .WithHttpLogging(HttpLoggingFields.All)
            .DisableAntiforgery()
            .Produces<UploadAvatarResult>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return route;
    }

    /// <summary>
    /// 上传用户头像
    /// </summary>
    private static async Task<IResult> UploadAvatarAsync(
        HttpContext context,
       [FromServices] IdentityServicesDi identityService,
        [FromForm] IFormFile file)
    {
        try
        {
            // ── 认证校验（S-13：统一 NameIdentifier Claim）──
            var userId = IdentityApiHelpers.TryGetAuthenticatedUserId(context);
            if (userId is null)
                return Results.Json(new { error = "用户未认证" }, statusCode: StatusCodes.Status401Unauthorized);

            // ── 文件校验 ──
            if (file is null || file.Length == 0)
                return Results.Json(new { error = "请选择要上传的图片" }, statusCode: StatusCodes.Status400BadRequest);

            if (file.Length > MaxFileSize)
                return Results.Json(
                    new { error = $"图片大小不能超过 {MaxFileSize / 1024 / 1024}MB" },
                    statusCode: StatusCodes.Status400BadRequest);

            // ── 读取文件内容 ──
            byte[] imageContent;
            await using (var ms = new MemoryStream())
            {
                await file.CopyToAsync(ms);
                imageContent = ms.ToArray();
            }

            // ── 构建命令（S-08：携带原始 Bearer token 供 FileDev gRPC 认证）──
            var authHeader = context.Request.Headers.Authorization.ToString();
            var accessToken = authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? authHeader["Bearer ".Length..].Trim()
                : null;

            var command = new UploadAvatarCommand(
                userId.Value,
                file.FileName,
                imageContent,
                file.ContentType,
                accessToken);

            // S-14：幂等键由客户端显式传入（X-Idempotency-Key），缺失时回退随机键
            var identifiedCommand = new IdentifiedCommand<UploadAvatarCommand, UploadAvatarResult>(
                IdentityApiHelpers.GetIdempotencyKey(context), command);

            // ── 发送命令 ──
            var result = await identityService.NotMediator.SendAsync(identifiedCommand);

            if (string.IsNullOrEmpty(result.FileId))
                return Results.Json(new { error = "上传失败，请重试" }, statusCode: StatusCodes.Status500InternalServerError);

            identityService.Logger.LogInformation(
                "[AvatarApi] 头像上传成功: UserId={UserId}, FileId={FileId}",
                userId.Value, result.FileId);

            return Results.Ok(new
            {
                message = "头像上传成功",
                fileId = result.FileId,
                fileUri = result.FileUri,
                width = result.Width,
                height = result.Height,
                format = result.Format
            });
        }
        catch (InvalidOperationException ex)
        {
            identityService.Logger.LogWarning(ex, "[AvatarApi] 头像上传业务校验失败");
            return Results.Json(new { error = ex.Message }, statusCode: StatusCodes.Status400BadRequest);
        }
        catch (Exception ex)
        {
            identityService.Logger.LogError(ex, "[AvatarApi] 头像上传异常");
            return Results.Json(
                new { error = "服务器内部错误，请稍后重试" },
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}
