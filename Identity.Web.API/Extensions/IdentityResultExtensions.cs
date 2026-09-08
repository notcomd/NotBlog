using Identity.Domain.Result;

namespace Identity.Web.API.Extensions;

/// <summary>
/// 将 Domain 层的 IdentityResult&lt;T&gt; 转换为 ASP.NET Core IResult 响应
/// 
/// 用于在 API 端点中返回领域结果时，无需在 Domain 层引用 Web 框架。
/// </summary>
public static class IdentityResultExtensions
{
    /// <summary>
    /// 将 IdentityResult&lt;T&gt; 映射为 IResult
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

        var response = new
        {
            message = result.ResultMessage,
            status = result.StatusCode.ToString(),
            data = result.ResultData
        };

        return Results.Json(response, statusCode: statusCode);
    }
}
