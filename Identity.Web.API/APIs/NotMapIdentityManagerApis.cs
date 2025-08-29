using System.Reflection.Metadata.Ecma335;

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

            route.MapPost("/ChangeWithUserSafetyAsync", ChangeWithUserSafetyAsync)
                .WithName("ChangeWithUserSafety")
                .WithHttpLogging(Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.All)
                .WithDisplayName("更新用户安全信息接口")
                .WithDescription("更新用户安全信息接口");

            route.MapPost("/ChangeWithUserPasswordAsync", ChangeWithUserPasswordAsync)
                .WithName("ChangeWithUserPassword")
                .WithHttpLogging(Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.All)
                .WithDisplayName("更新用户密码接口")
                .WithDescription("更新用户密码接口");


            route.MapPost("/RegisterWithRoleAsync", RegisterWithRoleAsync)
                .WithName("RegisterWithRole")
                .WithHttpLogging(Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.All)
                .WithDisplayName("注册角色接口")
                .WithDescription("注册角色接口");

            route.MapPost("/GetWithUserInforMetionAsync", GetWithUserInforMetionAsync)
                .WithName("GetWithUserInforMetion")
                .WithHttpLogging(Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.All)
                .WithDisplayName("获取用户信息接口")
                .WithDescription("获取用户信息接口");

            return routeGroupBuilder;
        }


        public static async Task<IdentityResult<string>> ChangeWithUserInfomationAsync([AsParameters] IdentityService identityService,
            RequestChangeWithUserInfomationModel requestChangeWithUserInfomationModel, CancellationToken cancellationToken)
        {
            if (requestChangeWithUserInfomationModel is null) throw new ArgumentNullException(nameof(requestChangeWithUserInfomationModel));

            var isUpdate = await identityService.NotMediator.SendAsync(new ChangeByUserCommand(requestChangeWithUserInfomationModel.UserEmail,
                  requestChangeWithUserInfomationModel.UserName, requestChangeWithUserInfomationModel.Address,
                  requestChangeWithUserInfomationModel.ImageUri), cancellationToken);
            if (!isUpdate)
            {
                return await IdentityResult<string>.ResultAsync("用户信息更新失败", EnumStatusCode.Error, "操作失败");
            }
            await identityService.UserRepository.UnitOfWork.SavaChangesAsync(cancellationToken);
            return await IdentityResult<string>.ResultAsync("用户信息更新成功", EnumStatusCode.Ok, "操作成功");
        }


        public static async ValueTask<IdentityResult<string>> ChangeWithUserSafetyAsync([AsParameters] IdentityService identityService,
            RequestChangeWithSafetyInformetionModel requestChangeWithSafetyInformetionModel, CancellationToken cancellationToken)
        {
            if (requestChangeWithSafetyInformetionModel is null) throw new ArgumentNullException(nameof(requestChangeWithSafetyInformetionModel));
            var isUpdate = await identityService.NotMediator.SendAsync(new ChangeByUserSafetyCommand(requestChangeWithSafetyInformetionModel.UserEmail,
                (EnBlackOrWhite)requestChangeWithSafetyInformetionModel.BlackOrWhite, (EnUserStatus)requestChangeWithSafetyInformetionModel.UserStatus
                ), cancellationToken);
            if (!isUpdate)
            {
                return await IdentityResult<string>.ResultAsync("用户安全信息更新失败", EnumStatusCode.Error, "操作失败");
            }
            await identityService.UserRepository.UnitOfWork.SavaChangesAsync(cancellationToken);
            return await IdentityResult<string>.ResultAsync("用户安全信息更新成功", EnumStatusCode.Ok, "操作成功");
        }



        public static async ValueTask<IdentityResult<string>> ChangeWithUserPasswordAsync([AsParameters] IdentityService identityService,
            RequestChangeWithPassworrdInformetionModel requestChangeWithPassworrdInformetionModel, CancellationToken cancellationToken)
        {
            if (requestChangeWithPassworrdInformetionModel is null) throw new ArgumentNullException(nameof(requestChangeWithPassworrdInformetionModel));
            var isUpdate = await identityService.NotMediator.SendAsync(new ChangeByPasswodCommand(requestChangeWithPassworrdInformetionModel.UserEmail,
                requestChangeWithPassworrdInformetionModel.OldPassword, requestChangeWithPassworrdInformetionModel.NewPassword), cancellationToken);
            return isUpdate
                 ? await IdentityResult<string>.ResultAsync("更新成功", EnumStatusCode.Ok, "操作成功")
                 : await IdentityResult<string>.ResultAsync("更新失败", EnumStatusCode.Error, "操作失败");
        }


        public static async ValueTask<IdentityResult<string>> RegisterWithRoleAsync([AsParameters] IdentityService identityService,
           RequestRegisterWithRoleModel requestRegisterrWithRoleModel, CancellationToken cancellationToken)
        {
            try
            {
                if (requestRegisterrWithRoleModel is null) throw new ArgumentNullException(nameof(requestRegisterrWithRoleModel));
                var signal = await identityService.NotMediator.SendAsync(new CreateByRoleCommand(requestRegisterrWithRoleModel.RoleName,
                     requestRegisterrWithRoleModel.Attribute, (EnRoleAuthority)requestRegisterrWithRoleModel.RoleAuthority, (EnRoleStatus)requestRegisterrWithRoleModel.RoleStatus), cancellationToken);
                return signal
                    ? await IdentityResult<string>.ResultAsync("注册成功，可以登录啦！", EnumStatusCode.Ok, "操作成功")
                    : await IdentityResult<string>.ResultAsync("注册失败，角色可能已存在", EnumStatusCode.Error, "操作失败");
            }
            catch (Exception ex)
            {
                return await IdentityResult<string>.ResultAsync($"注册失败，出现异常{ex.Message}", EnumStatusCode.Error, "操作失败");
            }
        }


        public async static ValueTask<IdentityResult<ResultWIthUserDto>> GetWithUserInforMetionAsync([AsParameters] IdentityService identityService,
            RequestWithUserInformetionModel requestWithUserInformetion, CancellationToken cancellationToken)
        {
            var userData=await identityService.IdentityDomainUserManagerServer.GetWithUserAsync(requestWithUserInformetion.FindByEmail,cancellationToken);
            if(userData is null)
            {
                return await IdentityResult<ResultWIthUserDto>.ResultAsync(null, EnumStatusCode.Error, "未找到用户信息");
            }
            return await IdentityResult<ResultWIthUserDto>.ResultAsync(userData, EnumStatusCode.Ok, "操作成功");
        }
    }
}
