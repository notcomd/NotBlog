


using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Notcomd.Identity.Server.HostServer;
using Notcomd.Identity.Server.Mapper;
using Notcomd.Identity.Server.Module;
using Notcomd.Token.JWT;

namespace Notcomd.Identity.Server
{
    public static class Notcomd_IdentityServer_Options
    {
        public static IServiceCollection AddIdentityServerConfig(this IServiceCollection services)
        {

            services.AddScoped<INotcomd_Mapper_Server,Notcomd_Mapper_Server>();
            services.AddScoped<INotcomd_Original_User,Notcomd_Original_User>();
            services.AddHostedService<Notcomd_HostServer>();
            //services.Configure<Notcomd_JwtToken_Configural>(opt=>opt.)
            services.AddDbContext<Notcomd_Identity_DbContext>().AddDbContext<Notcomd_Identity_User_Image_DbContext>();


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

        public static IServiceCollection AddIdentityServerConfig(this IServiceCollection services,IConfiguration configuration)
        {
            
            return services;
        }
    }
}
