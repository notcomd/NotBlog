using Commons.Result;
using Identity.Domain.Result;

namespace Identity.Web.API.Extensions;

/// <summary>
/// 将 Domain 层的 IdentityResult&lt;T&gt; 转换为 ASP.NET Core IResult 响应
/// 
/// 用于在 API 端点中返回领域结果时，无需在 Domain 层引用 Web 框架。
/// 输出统一信封形状（ApiResponseResult），包装中间件据此透传，避免二次包装。
/// </summary>
public static class IdentityResultExtensions
{
    /// <summary>
    /// 将 IdentityResult&lt;T&gt; 映射为统一信封 IResult 响应
    /// </summary>
    public static IResult ToHttpResult<T>(this IdentityResult<T> result) where T : class
    {
        var statusCode = result.StatusCode switch
        {
            StatusCode.Ok => 200,
            StatusCode.Error => 400,
            StatusCode.TimeOut => 408,
            StatusCode.Reset => 205,
            StatusCode.NotAuthorized => 401,
            StatusCode.InternalServerError => 500,
            _ => 500
        };

        var isSuccess = result.StatusCode == StatusCode.Ok;
        var message = result.ResultMessage ?? (isSuccess ? "请求成功" : "请求失败");

        var response = isSuccess
            ? ApiResponseResult<T>.Success(result.ResultData, message, statusCode)
            : ApiResponseResult<T>.Failure(message, statusCode);

        return Results.Json(response, statusCode: statusCode);
    }
}
