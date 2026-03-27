using System.Reflection;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace DomainInfrastructure;

/// <summary>
/// 工作单元过滤器
/// 在 Action 执行成功后，自动对标记了 SaverDbContextAttribute 的 DbContext 执行 SaveChangesAsync
/// </summary>
public class UnitOfWorkFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var result = await next();

        // Action 执行异常时不保存数据库变更
        if (result.Exception != null) return;

        if (context.ActionDescriptor is not ControllerActionDescriptor actionDescriptor) return;


        var saverAttr = actionDescriptor.MethodInfo.GetCustomAttribute<SaverDbContextAttribute>();
        if (saverAttr == null) return;

        foreach (var dbCtxType in saverAttr.DbContextTypes)
        {
            if (context.HttpContext.RequestServices.GetService(dbCtxType) is DbContext dbContext)
            {
                await dbContext.SaveChangesAsync();
            }
        }
    }
}