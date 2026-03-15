namespace Identity.Web.API.ActionFilter;

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
            if (context.HttpContext.RequestServices.GetService(itm) is DbContext dbser)
                await dbser.SaveChangesAsync();
        }
    }
}