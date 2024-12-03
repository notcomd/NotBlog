using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Notcomd.Token.JWT;

namespace Identity.Web.API.ActionFilter;

public class UserLimitsOfAuthorityFilter : IAsyncActionFilter
{

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {

        var result = await next();
        if (result is not null)
        {
            return;
        }
        var authorization = context.ActionDescriptor as ControllerActionDescriptor;
        if (authorization is null)
        {
            return;
        }
        var method = authorization?.MethodInfo.GetCustomAttributes<UserLimitsOfAuthorityAttribute>();
        if (method is not null)
        {
            var authorizationData = context.HttpContext.Response.Headers["Authorization"];
            if (string.IsNullOrEmpty(authorizationData))
            {
                context.Result = new ContentResult
                {
                    StatusCode = StatusCodes.Status403Forbidden,
                    Content = "没有token!"
                };
                return;
            }
            var toKenMethod = context.HttpContext.RequestServices.GetService<IJwtTokenOptions>() ??
                              throw new ArgumentNullException($"date[{DateTime.UtcNow}:token解析异常]");
            var token = await toKenMethod.JwtSecurityTokenHandlerAsync(authorizationData);
            foreach (var item in method)
            {
                var limi = item.LimitsOfAuthority.ToString();
                if (limi != token.Claims[ClaimTypes.Authentication].ToString())
                {
                    context.Result = new ContentResult
                    {
                        StatusCode = StatusCodes.Status403Forbidden,
                        Content = "没有访问权限！"
                    };
                }
                else
                {
                    await next();
                }
            }
        }
        else
        {
            return;
        }
    }
}