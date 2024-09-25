using System.Net;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Identity.Domain.Server;

public class UserLoginAsyncActionFilter: IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var result = await next();
        //return Task.CompletedTask;
    }
}