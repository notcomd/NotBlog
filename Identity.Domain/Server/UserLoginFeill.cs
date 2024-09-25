using System.Net;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Identity.Domain.Server;

public class UserLoginAsyncActionFilter: IAsyncActionFilter
{
    

    public Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        throw new NotImplementedException();
    }
}