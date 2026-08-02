using Identity.Web.API.Application.Commands;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Mvc;

namespace Identity.Web.API.APIs;

public static class RoleApi
{
    public static RouteGroupBuilder MapRoleApi(this RouteGroupBuilder routeBuilder)
    {
        var route = routeBuilder
            .MapGroup("/role")
            .WithHttpLogging(HttpLoggingFields.All);

        route.MapPost(string.Empty, CreateRoleAsync)
            .RequireAuthorization("AdminOnly")
            .WithDescription("创建角色")
            .Produces<CreateRoleResult>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        route.MapPut("/{roleId:guid}", UpdateRoleAsync)
            .RequireAuthorization("AdminOnly")
            .WithDescription("更新角色")
            .Produces<bool>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        route.MapDelete("/{roleId:guid}", DeleteRoleAsync)
            .RequireAuthorization("AdminOnly")
            .WithDescription("删除角色（软删除）")
            .Produces<bool>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return route;
    }

    private static async Task<IResult> CreateRoleAsync(
        [FromServices] INotMediator mediator,
        [FromBody] CreateRoleCommand command,
        HttpContext httpContext)
    {
        try
        {
            var identityCommand = new IdentifiedCommand<CreateRoleCommand, CreateRoleResult>(
                IdentityApis.GetIdempotencyKey(httpContext), command);
            var result = await mediator.SendAsync(identityCommand);

            return string.IsNullOrEmpty(result.RoleName)
                ? Results.Problem("创建失败，请重试", statusCode: 500)
                : Results.Created($"/api/identity/role/{result.RoleGuid}", result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> UpdateRoleAsync(
        [FromServices] INotMediator mediator,
        [FromRoute] Guid roleId,
        [FromBody] UpdateRoleCommand update,
        HttpContext httpContext)
    {
        try
        {
            var command = update with { RoleGuid = roleId };
            var identityCommand = new IdentifiedCommand<UpdateRoleCommand, bool>(
                IdentityApis.GetIdempotencyKey(httpContext), command);
            var result = await mediator.SendAsync(identityCommand);
            return Results.Ok(new { success = result });
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> DeleteRoleAsync(
        [FromServices] INotMediator mediator,
        [FromRoute] Guid roleId,
        HttpContext httpContext)
    {
        try
        {
            var command = new DeleteRoleCommand(roleId);
            var identityCommand = new IdentifiedCommand<DeleteRoleCommand, bool>(
                IdentityApis.GetIdempotencyKey(httpContext), command);
            var result = await mediator.SendAsync(identityCommand);
            return Results.Ok(new { success = result });
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }
}
