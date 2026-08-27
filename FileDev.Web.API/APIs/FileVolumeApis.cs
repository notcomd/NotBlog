using FileDev.Domain.IServices;
using Microsoft.AspNetCore.Mvc;

namespace FileDev.Web.API.APIs;

/// <summary>
/// 数据卷管理 API（仅管理员/系统调用，挂载在 <c>/api/filestorage</c> 需 JWT 认证的分组下）。
/// 提供卷清单查询、Lite 注册表全量同步、共享/租户专属卷新增、目录统计与存储层调整。
/// </summary>
public static class FileVolumeApis
{
    public static RouteGroupBuilder MapFileVolumeApis(this RouteGroupBuilder routeGroupBuilder)
    {
        var router = routeGroupBuilder.MapGroup("/volume");
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

    private static IResult BadRequest(string error) => Results.Json(new { ok = false, error }, statusCode: 400);

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
                return storageService.ChangeStorageTierAsync(path, request?.Tier ?? Domain.Enum.StorageTier.Hot, ct);
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
            return Results.Json(new { ok = false, error = "请求处理失败" }, statusCode: 500);
        }
    }



    public record AddSharedVolumeRequest(string RootPath);

    public record AddTenantVolumeRequest(string TenantId, string RootPath);

    public record ChangeTierRequest(Domain.Enum.StorageTier? Tier);
}