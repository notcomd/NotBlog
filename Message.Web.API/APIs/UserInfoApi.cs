namespace Message.Web.API.APIs;

/// <summary>
/// 用户资料接口（静态函数模式 + CQRS）。
/// <para>用户等级 / 硬币余额 / 背景封面（区别于头像）；写操作均为 upsert 语义（首次自动创建资料）。</para>
/// </summary>
public static class UserInfoApi
{
    /// <summary>映射用户资料端点组</summary>
    public static RouteGroupBuilder MapUserInfoApi(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/user-info")
            .WithTags("UserInfo")
            .RequireAuthorization();

        // GET /me — 我的用户资料（不存在返回默认等级1/硬币0）
        group.MapGet("/me", GetMyInfoAsync)
            .WithSummary("我的用户资料")
            .WithDescription("获取当前用户的等级、硬币余额、背景封面；未创建过资料时返回默认值")
            .Produces<ApiResponse<UserInfoDto>>();

        // PUT /me/background — 更新背景封面（空白清除）
        group.MapPut("/me/background", UpdateBackgroundAsync)
            .WithSummary("更新背景封面")
            .Accepts<UpdateBackgroundCoverRequest>("application/json")
            .Produces<ApiResponse>();

        // POST /me/coins/add — 增加硬币
        group.MapPost("/me/coins/add", AddCoinsAsync)
            .WithSummary("增加硬币")
            .Accepts<CoinAmountRequest>("application/json")
            .Produces<ApiResponse>();

        // POST /me/coins/consume — 扣除硬币（余额不足 400）
        group.MapPost("/me/coins/consume", ConsumeCoinsAsync)
            .WithSummary("扣除硬币")
            .Accepts<CoinAmountRequest>("application/json")
            .Produces<ApiResponse>();

        return group;
    }

    private static async Task<IResult> GetMyInfoAsync(
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var dto = await mediator.SendAsync(new GetMyUserInfoQuery(currentUser.GetUserId()), ct);
            return Results.Ok(ApiResponse<UserInfoDto>.Ok(dto));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse<UserInfoDto>.Error($"获取用户资料失败: {ex.Message}"), statusCode: 500);
        }
    }

    private static async Task<IResult> UpdateBackgroundAsync(
        [FromBody] UpdateBackgroundCoverRequest request,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            await mediator.SendAsync(new UpdateBackgroundCoverCommand(currentUser.GetUserId(), request.BackgroundCoverUrl), ct);
            return Results.Ok(ApiResponse.Ok("背景封面已更新"));
        }
        catch (ArgumentException ex)
        {
            return Results.Ok(ApiResponse.BadRequest(ex.Message));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse.Error($"更新背景封面失败: {ex.Message}"), statusCode: 500);
        }
    }

    private static async Task<IResult> AddCoinsAsync(
        [FromBody] CoinAmountRequest request,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            await mediator.SendAsync(new AddUserCoinsCommand(currentUser.GetUserId(), request.Amount), ct);
            return Results.Ok(ApiResponse.Ok("硬币已增加"));
        }
        catch (ArgumentException ex)
        {
            return Results.Ok(ApiResponse.BadRequest(ex.Message));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse.Error($"增加硬币失败: {ex.Message}"), statusCode: 500);
        }
    }

    private static async Task<IResult> ConsumeCoinsAsync(
        [FromBody] CoinAmountRequest request,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            await mediator.SendAsync(new ConsumeUserCoinsCommand(currentUser.GetUserId(), request.Amount), ct);
            return Results.Ok(ApiResponse.Ok("硬币已扣除"));
        }
        catch (ArgumentException ex)
        {
            return Results.Ok(ApiResponse.BadRequest(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Results.Ok(ApiResponse.BadRequest(ex.Message));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse.Error($"扣除硬币失败: {ex.Message}"), statusCode: 500);
        }
    }
}
