using System.Reflection;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace Identity.Web.API;

public class UnitOfWorkFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var result = await next();
        if (result.Exception != null) return;
        var action = context.ActionDescriptor as ControllerActionDescriptor;
        if (action is null) return;
        var meth = action.MethodInfo.GetCustomAttribute<SeverDbContextAttribute>();
        if (meth is null) return;
        foreach (var itm in meth.DbContextTypes)
        {
            var dbser = context.HttpContext.RequestServices.GetService(itm) as DbContext;
            if (dbser is not null)
                await dbser.SaveChangesAsync();
        }
    }
}