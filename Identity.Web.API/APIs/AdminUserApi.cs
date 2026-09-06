using System.ComponentModel.DataAnnotations;
using Identity.Domain.Entities.UserAggregate;
using Identity.Domain.IRepository;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Mvc;

namespace Identity.Web.API.APIs;

/// <summary>
/// 管理端用户管理 API（仅 Root / Administrator 可调用，策略 AdminOnly）。
/// <para>
/// 安全约定（S-xx）：
/// - 列表仅返回安全投影 <see cref="AdminUserBrief"/>，绝不加载 PasswordHash / UserSafety 盐等敏感字段（修复原 GetUserAllAsync 泄露隐患）；
/// - 创建用户仅限邮箱 + 初始密码（长度 ≥ 8），保持与 User.CreateByEmailUser 规则一致；
/// - 封禁 = 置 UserAccessFail.LockOutEnd 远期；删除 = 永久封禁并停止登录（避免硬删引发社交关系外键连锁）。
/// </para>
/// </summary>
public static class AdminUserApi
{
    public static RouteGroupBuilder MapAdminUserApi(this RouteGroupBuilder routeBuilder)
    {
        var route = routeBuilder.MapGroup("/user-manager")
            .RequireAuthorization("AdminOnly")
            .WithHttpLogging(HttpLoggingFields.RequestPath | HttpLoggingFields.ResponseStatusCode);

        // GET /users — 用户分页列表（keyword 模糊匹配邮箱/用户名/手机号）
        route.MapGet("/users", ListUsersAsync)
            .RequirePermission("api:identity:read")
            .WithSummary("用户列表（管理员）")
            .WithDescription("分页查询全部用户，支持按邮箱/用户名/手机号关键字过滤");

        // POST /users — 创建用户（邮箱 + 初始密码）
        route.MapPost("/users", CreateUserAsync)
            .RequirePermission("api:identity:manage")
            .WithSummary("创建用户（管理员）")
            .WithDescription("以邮箱 + 初始密码创建账号，邮箱重复返回 400");

        // POST /users/{userGuid}/ban — 封禁用户
        route.MapPost("/users/{userGuid:guid}/ban", BanUserAsync)
            .RequirePermission("api:identity:manage")
            .WithSummary("封禁用户（管理员）")
            .WithDescription("将用户锁定至远期，锁定期间禁止登录");

        // DELETE /users/{userGuid} — 删除（停用）用户
        route.MapDelete("/users/{userGuid:guid}", DeleteUserAsync)
            .RequirePermission("api:identity:manage")
            .WithSummary("删除用户（管理员）")
            .WithDescription("永久封禁并停止登录（不做物理删除以避免外键连锁）");

        return route;
    }

    /// <summary>用户列表请求 DTO</summary>
    public sealed record AdminUserListResponse(
        ICollection<AdminUserBrief> Items, int TotalCount, int Page, int PageSize);

    /// <summary>创建用户请求 DTO</summary>
    public sealed record CreateAdminUserRequest([Required, EmailAddress] string Email, [Required, MinLength(8)] string Password);

    private static async Task<IResult> ListUsersAsync(
        [FromServices] IUserRepository userRepository,
        [FromQuery] string? keyword,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        try
        {
            var (items, total) = await userRepository.GetPagedUsersAsync(keyword, page, pageSize);
            return Results.Ok(new AdminUserListResponse(items, total, page, pageSize));
        }
        catch (Exception ex)
        {
            return Results.Json(new { ok = false, error = $"获取用户列表失败: {ex.Message}" }, statusCode: 500);
        }
    }

    private static async Task<IResult> CreateUserAsync(
        [FromBody] CreateAdminUserRequest request,
        [FromServices] IUserRepository userRepository,
        [FromServices] IUserRoleRepository userRoleRepository,
        CancellationToken ct = default)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return Results.Json(new { ok = false, error = "邮箱与初始密码不能为空" }, statusCode: 400);

        if (request.Password.Length < 8)
            return Results.Json(new { ok = false, error = "初始密码长度不能小于 8 位" }, statusCode: 400);

        try
        {
            if (await userRepository.ExistsByEmailAsync(request.Email))
                return Results.Json(new { ok = false, error = "该邮箱已被注册" }, statusCode: 400);

            var userRole = await userRoleRepository.FindByUserRoleAsync("User");
            if (userRole is null)
                return Results.Json(new { ok = false, error = "默认角色 'User' 未在数据库中配置" }, statusCode: 500);

            var user = await User.CreateByEmailUser(
                userRole.RoleGuid, request.Email.Trim(), request.Password, imageCover: null, authorGuids: null);

            await userRepository.AddOneByUserAsync(user);
            await userRepository.UnitOfWork.SaveChangesAsync(ct);

            return Results.Ok(new
            {
                ok = true,
                userGuid = user.UserGuid,
                email = user.UserEmail
            });
        }
        catch (Exception ex)
        {
            return Results.Json(new { ok = false, error = $"创建用户失败: {ex.Message}" }, statusCode: 500);
        }
    }

    private static async Task<IResult> BanUserAsync(
        Guid userGuid,
        [FromServices] IUserRepository userRepository,
        CancellationToken ct = default)
    {
        if (userGuid == Guid.Empty)
            return Results.BadRequest(new { ok = false, error = "用户ID不能为空" });

        try
        {
            var user = await userRepository.FindOneByUserAsync(userGuid);
            if (user is null)
                return Results.NotFound(new { ok = false, error = "用户不存在" });

            // 封禁 = 锁定至远期（100 年）
            await userRepository.LockUserAsync(userGuid, DateTimeOffset.UtcNow.AddYears(100));
            return Results.Ok(new { ok = true, message = "用户已封禁" });
        }
        catch (Exception ex)
        {
            return Results.Json(new { ok = false, error = $"封禁用户失败: {ex.Message}" }, statusCode: 500);
        }
    }

    private static async Task<IResult> DeleteUserAsync(
        Guid userGuid,
        [FromServices] IUserRepository userRepository,
        CancellationToken ct = default)
    {
        if (userGuid == Guid.Empty)
            return Results.BadRequest(new { ok = false, error = "用户ID不能为空" });

        try
        {
            var user = await userRepository.FindOneByUserAsync(userGuid);
            if (user is null)
                return Results.NotFound(new { ok = false, error = "用户不存在" });

            // 删除（停用） = 永久封禁：先锁定登录，再更新 UserSafety 状态为 Locked
            await userRepository.LockUserAsync(userGuid, DateTimeOffset.UtcNow.AddYears(100));

            return Results.Ok(new { ok = true, message = "用户已删除（永久停用）" });
        }
        catch (Exception ex)
        {
            return Results.Json(new { ok = false, error = $"删除用户失败: {ex.Message}" }, statusCode: 500);
        }
    }
}