using Identity.Web.API.Application.Commands;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Mvc;

namespace Identity.Web.API.APIs;

public static class RoleGroupApi
{
    public static RouteGroupBuilder MapRoleGroupApi(this RouteGroupBuilder routeBuilder)
    {
        var route = routeBuilder
            .MapGroup("/rolegroup")
            .WithHttpLogging(HttpLoggingFields.All);

        route.MapPost(string.Empty, CreateRoleGroupAsync)
            .RequirePermission("api:identity:manage")
            .RequireAuthorization("AdminOnly")
            .WithDescription("创建角色组")
            .Produces<CreateRoleGroupResult>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        route.MapPut("/{groupId:guid}", UpdateRoleGroupAsync)
            .RequirePermission("api:identity:manage")
            .RequireAuthorization("AdminOnly")
            .WithDescription("更新角色组")
            .Produces<bool>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        route.MapDelete("/{groupId:guid}", DeleteRoleGroupAsync)
            .RequirePermission("api:identity:manage")
            .RequireAuthorization("AdminOnly")
            .WithDescription("删除角色组（软删除）")
            .Produces<bool>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return route;
    }

    private static async Task<IResult> CreateRoleGroupAsync(
        [FromServices] INotMediator mediator,
        [FromBody] CreateRoleGroupCommand command,
        HttpContext httpContext)
    {
        try
        {
            var identityCommand = new IdentifiedCommand<CreateRoleGroupCommand, CreateRoleGroupResult>(
                IdentityApis.GetIdempotencyKey(httpContext), command);
            var result = await mediator.SendAsync(identityCommand);

            return string.IsNullOrEmpty(result.RoleGroupName)
                ? Results.Problem("创建失败，请重试", statusCode: 500)
                : Results.Created($"/api/identity/rolegroup/{result.RoleGroupGuid}", result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> UpdateRoleGroupAsync(
        [FromServices] INotMediator mediator,
        [FromRoute] Guid groupId,
        [FromBody] UpdateRoleGroupCommand update,
        HttpContext httpContext)
    {
        try
        {
            var command = update with { RoleGroupGuid = groupId };
            var identityCommand = new IdentifiedCommand<UpdateRoleGroupCommand, bool>(
                IdentityApis.GetIdempotencyKey(httpContext), command);
            var result = await mediator.SendAsync(identityCommand);
            return Results.Ok(new { success = result });
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> DeleteRoleGroupAsync(
        [FromServices] INotMediator mediator,
        [FromRoute] Guid groupId,
        HttpContext httpContext)
    {
        try
        {
            var command = new DeleteRoleGroupCommand(groupId);
            var identityCommand = new IdentifiedCommand<DeleteRoleGroupCommand, bool>(
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
