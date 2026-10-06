using Commons.Result;
using FileDev.Domain.IServices;
using Microsoft.AspNetCore.Mvc;

namespace FileDev.Web.API.APIs;

/// <summary>
/// 数据卷管理 API（仅 Root / 管理员，挂载在 <c>/api/filestorage</c> 需 JWT 认证的分组下）。
/// 提供卷清单查询、存储卷统计同步、共享/租户专属卷新增、目录统计与存储层调整。
/// <para>
/// 安全约定（双层校验，与 Identity 管理端点一致）：
/// - 码层：继承父组 <c>/api/filestorage</c> 的 <c>RequireResourcePermissions("api:file")</c>；
/// - 角色层：本组额外要求 <c>AdminOnly</c> 策略（Root / Administrator / Admin）。
/// </para>
/// <para>
/// ⚠️ 为什么必须补角色层：<c>api:file:*</c> 是**普通用户默认持有**的权限码，仅靠码层会让任何已登录用户
/// 都能枚举存储卷、新增共享/租户卷、调整存储层。本组端点无内部 HTTP 调用方
/// （<c>VolumeSyncBackgroundService</c> 等后台任务直接调用服务层而非走 HTTP），故收紧不影响系统自身运行。
/// </para>
/// </summary>
public static class FileVolumeApi
{
    public static RouteGroupBuilder MapFileVolumeApi(this RouteGroupBuilder routeGroupBuilder)
    {
        // 组级施加，覆盖本组全部端点（含后续新增），避免逐端点漏标
        var router = routeGroupBuilder.MapGroup("/volume")
            .RequireAuthorization("AdminOnly");
        router.MapGet("/", ListVolumesAsync);
        router.MapGet("/{volumeId}", GetVolumeAsync);
        router.MapPost("/sync", SyncVolumesAsync);
        router.MapPost("/shared", AddSharedVolumeAsync);
        router.MapPost("/tenant", AddTenantVolumeAsync);
        router.MapGet("/dirs", GetDirectoryStatsAsync);
        router.MapPost("/{path}/tier", ChangeTierAsync)
            .WithMetadata(new IgnoreAntiforgeryTokenAttribute());
        return router;
    }

    private static IResult BadRequest(string error) => Results.Json(ApiResponseResult.Failure(error, 400), statusCode: 400);

    private static Task<IResult> ListVolumesAsync(
        [FromServices] INotFileVolumeService volumeService,
        CancellationToken ct) =>
        ExecuteAsync(() => volumeService.GetVolumesAsync(ct), v => Results.Json(new { ok = true, volumes = v }));

    private static Task<IResult> GetVolumeAsync(
        [FromServices] INotFileVolumeService volumeService,
        string volumeId,
        CancellationToken ct) =>
        ExecuteAsync(
            () => volumeService.GetVolumeAsync(volumeId, ct),
            v => v is null ? BadRequest($"卷不存在：{volumeId}") : Results.Json(new { ok = true, volume = v }));

    private static Task<IResult> SyncVolumesAsync(
        [FromServices] INotFileVolumeService volumeService,
        CancellationToken ct) =>
        ExecuteAsync(
            () => volumeService.SyncVolumesAsync(ct),
            count => Results.Json(new { ok = true, synced = count }));

    private static Task<IResult> AddSharedVolumeAsync(
        [FromServices] INotFileVolumeService volumeService,
        [FromBody] AddSharedVolumeRequest request,
        CancellationToken ct) =>
        ExecuteAsync(
            () =>
            {
                if (string.IsNullOrWhiteSpace(request?.RootPath))
                    throw new ArgumentException("rootPath 不能为空");
                return volumeService.AddSharedVolumeAsync(request.RootPath, ct);
            },
            v => Results.Json(new { ok = true, volume = v }));

    private static Task<IResult> AddTenantVolumeAsync(
        [FromServices] INotFileVolumeService volumeService,
        [FromBody] AddTenantVolumeRequest request,
        CancellationToken ct) =>
        ExecuteAsync(
            () =>
            {
                if (string.IsNullOrWhiteSpace(request?.TenantId))
                    throw new ArgumentException("tenantId 不能为空");
                if (string.IsNullOrWhiteSpace(request.RootPath))
                    throw new ArgumentException("rootPath 不能为空");
                return volumeService.AddTenantVolumeAsync(request.TenantId, request.RootPath, ct);
            },
            v => Results.Json(new { ok = true, volume = v }));

    private static Task<IResult> GetDirectoryStatsAsync(
        [FromServices] INotFileVolumeService volumeService,
        CancellationToken ct) =>
        ExecuteAsync(
            () => volumeService.GetDirectoryStatsAsync(ct),
            dirs => Results.Json(new { ok = true, directories = dirs }));

    private static Task<IResult> ChangeTierAsync(
        [FromServices] INotFileStorageService storageService,
        string path,
        [FromBody] ChangeTierRequest request,
        CancellationToken ct) =>
        ExecuteAsync(
            () =>
            {
                if (string.IsNullOrWhiteSpace(path))
                    throw new ArgumentException("path 不能为空");
                // 命名空间以对象属主（路径首段）为准，而非管理端调用者
                var tenantId = FileApiHelpers.ResolveTenantFromFileKey(path);
                return storageService.ChangeStorageTierAsync(path, request?.Tier ?? Domain.Enum.StorageTier.Hot, ct,
                    new StoreContext(null, tenantId));
            },
            m => m is null ? BadRequest($"对象不存在或调整失败：{path}") : Results.Json(new { ok = true, manifest = m }));

    /// <summary>统一执行与错误处理：领域异常 → 400，其余 → 500。</summary>
    private static async Task<IResult> ExecuteAsync<T>(Func<Task<T>> action, Func<T, IResult> onSuccess)
    {
        try
        {
            return onSuccess(await action().ConfigureAwait(false));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception)
        {
            return Results.Json(ApiResponseResult.Failure("请求处理失败", 500), statusCode: 500);
        }
    }

    public record AddSharedVolumeRequest(string RootPath);

    public record AddTenantVolumeRequest(string TenantId, string RootPath);

    public record ChangeTierRequest(Domain.Enum.StorageTier? Tier);
}