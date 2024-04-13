using Microsoft.AspNetCore.Builder;

namespace Notcomd.Evenbus
{
    public static class ApplicationsBuildOption
    {
        public static IApplicationBuilder UseNotcomdEvenbus(this IApplicationBuilder serviceProvider)
        {
            var appServe = serviceProvider.ApplicationServices.GetService(typeof(IEventBus));
            if (appServe == null)
            {
                throw new ApplicationException("^[x_x]^!!检查是否构建了实例");
            }
            return serviceProvider;
        }
    }
}