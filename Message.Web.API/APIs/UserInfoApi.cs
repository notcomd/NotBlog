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
            .RequireAuthorization()
            .RequireResourcePermissions("api:userinfo");

        // GET /me — 我的用户资料（不存在返回默认等级1/硬币0）
        group.MapGet("/me", GetMyInfoAsync)
            .WithSummary("我的用户资料")
            .WithDescription("获取当前用户的等级、硬币余额、背景封面；未创建过资料时返回默认值")
            .Produces<ApiResponseResult<UserInfoDto>>();

        // PUT /me/background — 更新背景封面（空白清除）
        group.MapPut("/me/background", UpdateBackgroundAsync)
            .WithSummary("更新背景封面")
            .Accepts<UpdateBackgroundCoverRequest>("application/json")
            .Produces<ApiResponseResult>();

        // PUT /me/bio — 更新个人签名（空白清除）
        group.MapPut("/me/bio", UpdateBioAsync)
            .WithSummary("更新个人签名")
            .Accepts<UpdateUserBioRequest>("application/json")
            .Produces<ApiResponseResult>();

        // POST /sign-in — 每日签到（+250 经验）
        group.MapPost("/sign-in", SignInAsync)
            .WithSummary("每日签到")
            .WithDescription("签到获得 250 经验并自动升级；每日一次，重复签到返回 400")
            .Produces<ApiResponseResult<SignInResultDto>>();

        // GET /me/sign-in-dates — 我的签到日期（热力图；默认近 365 天）
        group.MapGet("/me/sign-in-dates", GetMySignInDatesAsync)
            .WithSummary("我的签到日期")
            .WithDescription("查询当前用户在指定日期区间内的签到日期（默认近 365 天），并返回累计签到天数")
            .Produces<ApiResponseResult<SignInDatesDto>>();

        // POST /me/coins/add — 增加硬币
        group.MapPost("/me/coins/add", AddCoinsAsync)
            .WithSummary("增加硬币")
            .Accepts<CoinAmountRequest>("application/json")
            .Produces<ApiResponseResult>();

        // POST /me/coins/consume — 扣除硬币（余额不足 400）
        group.MapPost("/me/coins/consume", ConsumeCoinsAsync)
            .WithSummary("扣除硬币")
            .Accepts<CoinAmountRequest>("application/json")
            .Produces<ApiResponseResult>();

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
            return Results.Ok(ApiResponseResult<UserInfoDto>.Ok(dto));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponseResult<UserInfoDto>.Error($"获取用户资料失败: {ex.Message}"), statusCode: 500);
        }
    }

    private static async Task<IResult> SignInAsync(
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var result = await mediator.SendAsync(new SignInCommand(currentUser.GetUserId()), ct);
            return Results.Ok(ApiResponseResult<SignInResultDto>.Ok(result));
        }
        catch (InvalidOperationException ex)
        {
            return Results.Json(ApiResponseResult<SignInResultDto>.BadRequest(ex.Message), statusCode: 400);
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponseResult<SignInResultDto>.Error($"签到失败: {ex.Message}"), statusCode: 500);
        }
    }

    private static async Task<IResult> GetMySignInDatesAsync(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        var end = to ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var start = from ?? end.AddDays(-364);
        if (start > end)
            return Results.Json(ApiResponseResult<SignInDatesDto>.BadRequest("起始日期不能晚于结束日期"), statusCode: 400);

        try
        {
            var dto = await mediator.SendAsync(new GetSignInDatesQuery(currentUser.GetUserId(), start, end), ct);
            return Results.Ok(ApiResponseResult<SignInDatesDto>.Ok(dto));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponseResult<SignInDatesDto>.Error($"获取签到记录失败: {ex.Message}"), statusCode: 500);
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
            return Results.Ok(ApiResponseResult.Ok("背景封面已更新"));
        }
        catch (ArgumentException ex)
        {
            return Results.Json(ApiResponseResult.BadRequest(ex.Message), statusCode: 400);
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponseResult.Error($"更新背景封面失败: {ex.Message}"), statusCode: 500);
        }
    }

    private static async Task<IResult> UpdateBioAsync(
        [FromBody] UpdateUserBioRequest request,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] INotMediator mediator,
        CancellationToken ct)
    {
        try
        {
            await mediator.SendAsync(new UpdateUserBioCommand(currentUser.GetUserId(), request.Bio), ct);
            return Results.Ok(ApiResponseResult.Ok("个人签名已更新"));
        }
        catch (ArgumentException ex)
        {
            return Results.Json(ApiResponseResult.BadRequest(ex.Message), statusCode: 400);
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponseResult.Error($"更新个人签名失败: {ex.Message}"), statusCode: 500);
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
            return Results.Ok(ApiResponseResult.Ok("硬币已增加"));
        }
        catch (ArgumentException ex)
        {
            return Results.Json(ApiResponseResult.BadRequest(ex.Message), statusCode: 400);
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponseResult.Error($"增加硬币失败: {ex.Message}"), statusCode: 500);
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
            return Results.Ok(ApiResponseResult.Ok("硬币已扣除"));
        }
        catch (ArgumentException ex)
        {
            return Results.Json(ApiResponseResult.BadRequest(ex.Message), statusCode: 400);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Json(ApiResponseResult.BadRequest(ex.Message), statusCode: 400);
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponseResult.Error($"扣除硬币失败: {ex.Message}"), statusCode: 500);
        }
    }
}
