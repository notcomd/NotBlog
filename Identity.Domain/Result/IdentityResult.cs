using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Identity.Domain.Result;

/// <summary>
/// 领域结果对象 — 纯 POCO，不依赖任何 Web 框架
/// 
/// 在 Web.API 层通过扩展方法转换为 IResult 响应
/// </summary>
public sealed class IdentityResult<TResponse>
{
    public IdentityResult(string resultMessage, StatusCode statusCode, TResponse resultData)
    {
        ResultMessage = resultMessage;
        StatusCode = statusCode;
        ResultData = resultData;
    }

    /// <summary>
    ///     返回结果消息
    /// </summary>
    public string ResultMessage { get; set; }

    /// <summary>
    ///     状态码
    /// </summary>
    public StatusCode StatusCode { get; set; }

    /// <summary>
    ///     返回数据
    /// </summary>
    public TResponse? ResultData { get; set; }

}
