using System.Reflection;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace Notcomd.DomainCommand;

public class UnitOfWorkFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var result = await next();
        if (result.Exception != null) return;
        var action = context.ActionDescriptor as ControllerActionDescriptor;
        if (action is null) return;
        var meth = action.MethodInfo.GetCustomAttribute<SaverDbContextAttribute>();
        if (meth is null) return;
        foreach (var itm in meth.DbContextTypes)
            if (context.HttpContext.RequestServices.GetService(itm) is DbContext dbser)
                await dbser.SaveChangesAsync();
    }
}