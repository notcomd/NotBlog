using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Mvc;

namespace FileDev.Web.API.APIs;

public static class FileStrongApi
{
    public static RouteGroupBuilder FileStrongApis(this RouteGroupBuilder routeGroupBuilder)
    {

        var route = routeGroupBuilder.MapGroup("/filestorage").RequireAuthorization();
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
            return Results.Json(new { ok = false, error = "未认证" }, statusCode: 401);

        try
        {
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

            // S-14：幂等键由客户端请求头 X-Idempotency-Key 传入，客户端重试时携带同一键以去重；
            // 缺失/非法时回退随机键（该请求无幂等保证）
            var identityCreateCommand = new IdentifiedCommand<CreateNotFileGroupCommand, bool>(
                FileApiHelpers.GetIdempotencyKey(httpContext), command);

            await servicesDi. NotMediator.SendAsync(identityCreateCommand, cancellationToken);

            return Results.Json(new { ok = true });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "创建文件组失败: UserId={UserId}, Name={Name}", userGuid.Value, request?.Name);
            return Results.Json(new { ok = false, error = "请求处理失败" }, statusCode: 500);
        }
    }
}