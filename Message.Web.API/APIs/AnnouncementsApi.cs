using Message.Domain.Entities.Announcement;
using Message.Domain.IRepository;

namespace Message.Web.API.APIs;

/// <summary>
/// 公报 API（静态函数模式 + CQRS 读仓储直查）。
/// <para>
/// - 读侧：所有登录用户可看当前有效公报（分页）；
/// - 写侧：仅管理员可发布 / 撤回（currentUser.IsAdmin() 校验，与 AuditApi 一致）。
/// </para>
/// </summary>
public static class AnnouncementsApi
{
    public static RouteGroupBuilder MapAnnouncementsApi(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/announcements")
            .WithTags("Announcements")
            .RequireAuthorization();

        // GET / — 有效公报列表（分页，未撤回，按时间倒序）
        group.MapGet("/", GetActiveAnnouncementsAsync)
            .RequirePermission("api:notification:read")
            .WithSummary("有效公报列表")
            .WithDescription("获取当前有效的公报列表（不含已撤回），支持分页")
            .Produces<ApiResponseResult<PagedResult<object>>>();

        // POST / — 发布公报（管理员）
        group.MapPost("/", CreateAnnouncementAsync)
            .RequirePermission("api:audit:create")
            .WithSummary("发布公报（管理员）")
            .Accepts<CreateAnnouncementRequest>("application/json")
            .Produces<ApiResponseResult<Guid>>();

        // POST /{announcementGuid}/recall — 撤回公报（管理员）
        group.MapPost("/{announcementGuid:guid}/recall", RecallAnnouncementAsync)
            .RequirePermission("api:audit:create")
            .WithSummary("撤回公报（管理员）")
            .Produces<ApiResponseResult>();

        return group;
    }

    /// <summary>发布公报请求 DTO</summary>
    public sealed record CreateAnnouncementRequest(string Title, string Content);

    private static async Task<IResult> GetActiveAnnouncementsAsync(
        [FromServices] IAnnouncementRepository announcementRepository,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        try
        {
            var items = await announcementRepository.GetActiveAsync(page, pageSize);
            var total = await announcementRepository.GetActiveCountAsync();

            var dtos = items.Select(a => new
            {
                a.AnnouncementGuid,
                a.Title,
                a.Content,
                a.CreatorUserId,
                a.CreatedAt
            });

            return Results.Ok(ApiResponseResult<PagedResult<object>>.Ok(new PagedResult<object>
            {
                Items = [.. dtos.Cast<object>()],
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            }));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponseResult<PagedResult<object>>.Error($"获取公报列表失败: {ex.Message}"), statusCode: 500);
        }
    }

    private static async Task<IResult> CreateAnnouncementAsync(
        [FromBody] CreateAnnouncementRequest request,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] IAnnouncementRepository announcementRepository,
        CancellationToken ct)
    {
        try
        {
            if (!currentUser.IsAdmin())
                return Results.Json(ApiResponseResult.Forbidden("仅管理员可发布公报"), statusCode: 403);

            if (request is null || string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Content))
                return Results.BadRequest(ApiResponseResult<Guid>.BadRequest("标题与内容不能为空"));

            var announcement = Announcement.Create(currentUser.GetUserId(), request.Title, request.Content);
            await announcementRepository.AddAsync(announcement);
            await announcementRepository.UnitOfWork.SaveEntitiesAsync(ct);

            return Results.Ok(ApiResponseResult<Guid>.Created(announcement.AnnouncementGuid, "公报发布成功"));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponseResult<Guid>.Error($"发布公报失败: {ex.Message}"), statusCode: 500);
        }
    }

    private static async Task<IResult> RecallAnnouncementAsync(
        Guid announcementGuid,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] IAnnouncementRepository announcementRepository,
        CancellationToken ct)
    {
        try
        {
            if (!currentUser.IsAdmin())
                return Results.Json(ApiResponseResult.Forbidden("仅管理员可撤回公报"), statusCode: 403);

            var announcement = await announcementRepository.GetByIdAsync(announcementGuid);
            if (announcement is null)
                return Results.NotFound(ApiResponseResult.NotFound("公报不存在"));

            announcement.Recall();
            await announcementRepository.UpdateAsync(announcement);
            await announcementRepository.UnitOfWork.SaveEntitiesAsync(ct);

            return Results.Ok(ApiResponseResult.Ok("公报已撤回"));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponseResult.Error($"撤回公报失败: {ex.Message}"), statusCode: 500);
        }
    }
}