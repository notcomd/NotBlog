


using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

using Notcomd.Identity.Server.Mapper;
using Notcomd.Identity.Server.Module;

namespace Notcomd.Identity.Server
{
    public static class Notcomd_IdentityServer_Options
    {
        public static IServiceCollection IdentityServerConfig(this IServiceCollection services)
        {
            services.AddIdentityCore<Notcomd_User_Module>(opt =>
            {
                opt.Password.RequiredLength = 8;
                opt.Password.RequireUppercase = false;
                opt.Password.RequireNonAlphanumeric = false;
                opt.Password.RequireLowercase = false;
                opt.Password.RequireDigit = false;
                opt.Tokens.PasswordResetTokenProvider = TokenOptions.DefaultPhoneProvider;
                opt.Tokens.EmailConfirmationTokenProvider = TokenOptions.DefaultEmailProvider;
            });
            var identity = new IdentityBuilder(typeof(Notcomd_User_Module), typeof(Notcomd_Role_Module), services);
            identity.AddEntityFrameworkStores<Notcomd_Identity_DbContext>()
                .AddDefaultTokenProviders()
                .AddUserManager<UserManager<Notcomd_User_Module>>()
                .AddRoleManager<RoleManager<Notcomd_Role_Module>>();
            return services;
        }
    }
}
