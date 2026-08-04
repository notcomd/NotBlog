using FileDev.Web.API.Application.Command;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Mvc;

namespace FileDev.Web.API.APIs;

public static class FileStrongApi
{
    public static RouteGroupBuilder FileStrongApis(this RouteGroupBuilder routeGroupBuilder)
    {
        // S-08：文件组/文件上传端点要求认证
        var route = routeGroupBuilder.MapGroup("/filestorage").RequireAuthorization();
        // #30：移除无客户端调用的占位端点 /upload_file（原实现仅返回 { ok=true }，未真正上传）
        // Major：HttpLoggingFields.All 会记录完整请求体（含文件内容/敏感字段），改用 RequestPath + RequestQuery + Response
        route.MapPost("/create_file_group", CreateFileGroupAsync)
            .WithHttpLogging(HttpLoggingFields.RequestPath | HttpLoggingFields.RequestQuery
                             | HttpLoggingFields.ResponseStatusCode | HttpLoggingFields.ResponseHeaders, 1, 1);
        return route;
    }

    private static async Task<IResult> CreateFileGroupAsync(
        HttpContext httpContext,
        [FromServices] FileServicesDi servicesDi,
        [FromServices] ILoggerFactory loggerFactory,
        [FromBody] CreateFileGroupRequest request,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("FileStrongApi");
        var userGuid = FileApiHelpers.GetUserId(httpContext);
        if (userGuid == null)
            // #27：统一响应形状 { ok, error }
            return Results.Json(new { ok = false, error = "未认证" }, statusCode: 401);

        try
        {
            // Major：参数合法性校验前置
            if (request == null)
                return Results.Json(new { ok = false, error = "请求体不能为空" }, statusCode: 400);
            if (string.IsNullOrWhiteSpace(request.Name))
                return Results.Json(new { ok = false, error = "文件组名称不能为空" }, statusCode: 400);

            var command = new CreateNotFileGroupCommand()
            {
                UserGuid = userGuid.Value,
                FileGroupName = request.Name,
                FileGroupDescription = request.Description,
                FileGroupTags = request.GroupTags,
                FileIdentity = request.FileIdentity,
                ParentGroupId = request.ParentGroupId,
            };

            var identityCreateCommand = new IdentifiedCommand<CreateNotFileGroupCommand, bool>(Guid.CreateVersion7(), command);

            await servicesDi.NotMediator.SendAsync(identityCreateCommand, cancellationToken);

            return Results.Json(new { ok = true });
        }
        catch (Exception ex)
        {
            // #11：完整异常仅记录服务端日志，客户端返回安全通用消息
            logger.LogError(ex, "创建文件组失败: UserId={UserId}, Name={Name}", userGuid.Value, request?.Name);
            return Results.Json(new { ok = false, error = "请求处理失败" }, statusCode: 500);
        }
    }
}