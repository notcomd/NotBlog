using Identity.Web.API.Application.Command;
using Identity.Web.API.Application.Models;

namespace Identity.Web.API.APIs
{
    public static class NotMapIdentityManagerApis
    {
        public static RouteGroupBuilder NotMapIdentityManagerApi(this RouteGroupBuilder routeGroupBuilder)
        {
            var route = routeGroupBuilder.MapGroup("/api/identity/manager")
                .WithTags("IdentityManager").WithHttpLogging(Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.Request);

            route.MapPost("/ChangeWithUserInfomation", ChangeWithUserInfomationAsync)
                .WithName("ChangeWithUserInfomation")
                .WithHttpLogging(Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.All)
                .WithDisplayName("更新用户信息接口")
                .WithDescription("更新用户信息接口");

            return routeGroupBuilder;
        }


        public static async Task<IdentityResult<string>> ChangeWithUserInfomationAsync([AsParameters] IdentityService identityService,
            RequestChangeWithUserInfomationModel requestChangeWithUserInfomationModel, CancellationToken cancellationToken)
        {
            if (requestChangeWithUserInfomationModel is null) throw new ArgumentNullException(nameof(requestChangeWithUserInfomationModel));

           await identityService.NotMediator.SendAsync(new ChangeByUserCommand(requestChangeWithUserInfomationModel.UserEmail,
                requestChangeWithUserInfomationModel.UserName, requestChangeWithUserInfomationModel.Address,
                requestChangeWithUserInfomationModel.ImageUri), cancellationToken);

           return await IdentityResult<string>.ResultAsync("用户信息更新成功", EnumStatusCode.Ok, "操作成功");
        }

    }
}
