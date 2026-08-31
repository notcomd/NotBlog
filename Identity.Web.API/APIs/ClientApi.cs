using Identity.Domain.Dto.Response;
using Identity.Web.API.Application.Commands;
using Identity.Web.API.Application.Commands.Client;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Mvc;

namespace Identity.Web.API.APIs;

/// <summary>
/// OAuth 客户端（NotClient）管理端点 — 全部要求 AdminOnly 权限
///
/// 创建时服务端生成 client_id / client_secret，密钥仅返回一次；
/// 吊销后客户端立即无法在 /authorize 与 /token 使用。
/// </summary>
public static class ClientApi
{
    public static RouteGroupBuilder MapClientApi(this RouteGroupBuilder routeBuilder)
    {
        var route = routeBuilder
            .MapGroup("/client")
            .WithHttpLogging(HttpLoggingFields.All);

        route.MapPost(string.Empty, CreateClientAsync)
            .RequireAuthorization("AdminOnly")
            .WithDescription("创建 OAuth 客户端（client_secret 仅返回一次）")
            .Produces<CreateNotClientResult>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        route.MapGet(string.Empty, GetClientsAsync)
            .RequireAuthorization("AdminOnly")
            .WithDescription("获取全部 OAuth 客户端（不含密钥）")
            .Produces<IReadOnlyList<ClientInfoDto>>(StatusCodes.Status200OK);

        route.MapGet("/{clientId:guid}", GetClientAsync)
            .RequireAuthorization("AdminOnly")
            .WithDescription("获取客户端详情（不含密钥）")
            .Produces<ClientInfoDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        route.MapPut("/{clientId:guid}", UpdateClientAsync)
            .RequireAuthorization("AdminOnly")
            .WithDescription("更新客户端信息（null 字段不修改）")
            .Produces<bool>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        route.MapDelete("/{clientId:guid}", RevokeClientAsync)
            .RequireAuthorization("AdminOnly")
            .WithDescription("吊销客户端（不可恢复）")
            .Produces<bool>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return route;
    }

   

    private static async Task<IResult> CreateClientAsync(
        [FromServices] INotMediator mediator,
        [FromBody] CreateNotClientCommand command,
        HttpContext httpContext)
    {
        try
        {
            var identifiedCommand = new IdentifiedCommand<CreateNotClientCommand, CreateNotClientResult>(
                IdentityApis.GetIdempotencyKey(httpContext), command);
            var result = await mediator.SendAsync(identifiedCommand);

            return result.NotClientId == Guid.Empty
                ? Results.Problem("创建失败，请重试", statusCode: StatusCodes.Status500InternalServerError)
                : Results.Created($"/api/identity/client/{result.NotClientId}", result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> GetClientsAsync(
        [FromServices] INotClientRepository clientRepository,
        CancellationToken ct)
    {
        var clients = await clientRepository.GetAllAsync(ct);
        return Results.Ok(clients.Select(ToDto));
    }

    private static async Task<IResult> GetClientAsync(
        [FromServices] INotClientRepository clientRepository,
        [FromRoute] Guid clientId,
        CancellationToken ct)
    {
        var client = await clientRepository.FindByIdAsync(clientId, ct);
        return client is null
            ? Results.NotFound(new { error = $"客户端 '{clientId}' 不存在" })
            : Results.Ok(ToDto(client));
    }

    private static async Task<IResult> UpdateClientAsync(
        [FromServices] INotMediator mediator,
        [FromRoute] Guid clientId,
        [FromBody] UpdateNotClientCommand update,
        HttpContext httpContext)
    {
        try
        {
            var command = update with { NotClientId = clientId };
            var identifiedCommand = new IdentifiedCommand<UpdateNotClientCommand, bool>(
                IdentityApis.GetIdempotencyKey(httpContext), command);
            var result = await mediator.SendAsync(identifiedCommand);
            return Results.Ok(new { success = result });
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> RevokeClientAsync(
        [FromServices] INotMediator mediator,
        [FromRoute] Guid clientId,
        HttpContext httpContext)
    {
        try
        {
            var command = new RevokeNotClientCommand(clientId);
            var identifiedCommand = new IdentifiedCommand<RevokeNotClientCommand, bool>(
                IdentityApis.GetIdempotencyKey(httpContext), command);
            var result = await mediator.SendAsync(identifiedCommand);
            return Results.Ok(new { success = result });
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    

    private static ClientInfoDto ToDto(NotClient client)
    {
        return new ClientInfoDto(
            client.NotClientId,
            client.ClientId,
            client.ApplicationName,
            client.ApplicationDescription,
            client.ApplicationType.ToString(),
            client.TokenEndpointAuthMethod,
            client.Status.ToString(),
            client.RedirectUris,
            client.AllowedGrantTypes,
            client.AllowedScopes,
            client.RequirePkce,
            client.RequireConsent,
            client.CreatedAt,
            client.UpdatedAt);
    }
}
