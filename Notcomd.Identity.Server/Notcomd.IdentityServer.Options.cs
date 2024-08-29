using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notcomd.Identity.Server.Config;
using Notcomd.Identity.Server.Entity;
using Notcomd.Identity.Server.HostServer;
using Notcomd.Identity.Server.IServer;
using Notcomd.Identity.Server.Model;
using Notcomd.Identity.Server.Server;
using Notcomd.Token.JWT;

namespace Notcomd.Identity.Server
{
    public static class Notcomd_IdentityServer_Options
    {
  

        public static IServiceCollection AddIdentityServerConfig(this IServiceCollection services ,IConfiguration configuration)
        {
            services.AddDatabaseDeveloperPageExceptionFilter();
            services.AddScoped<INotcomd_Original_User, Notcomd_Original_User>();
            services.AddScoped<INotcomd_Identity_Factory, Notcomd_Identity_Factory>();
            services.AddScoped<INotcomd_ImageFactory, Notcomd_ImageFactory>();
            services.AddScoped<INotcomd_Append_Factory ,Notcomd_Append_Factory>();
            services.AddScoped<Notcomd_AppendServer>();
            services.AddScoped<Notcomd_SigInServer>();
            services.AddScoped<Notcomd_CreateServer>();
            services.AddHostedService<Notcomd_HostServer>();
            services.AddScoped<UserRole>();
            ///配置identity
            services.AddIdentityCore<Notcomd_User_Model>(opt =>
            {
                opt.Password.RequiredLength = 8;
                opt.Password.RequireUppercase = false;
                opt.Password.RequireNonAlphanumeric = false;
                opt.Password.RequireLowercase = false;
                opt.Password.RequireDigit = false;
                opt.Tokens.PasswordResetTokenProvider = TokenOptions.DefaultPhoneProvider;
                opt.Tokens.EmailConfirmationTokenProvider = TokenOptions.DefaultEmailProvider;
            });
            var identity = new IdentityBuilder(typeof(Notcomd_User_Model), typeof(Notcomd_Role_Model), services);
            identity.AddEntityFrameworkStores<Notcomd_Identity_DbContext>()
                .AddDefaultTokenProviders()
                .AddUserManager<UserManager<Notcomd_User_Model>>()
                .AddRoleManager<RoleManager<Notcomd_Role_Model>>();
            services.AddJwtAuthentication(configuration);
            //services.AddAuthentication();
            services.AddAuthorization();
            return services;
        }


       public static void AddDbContextOptions(this IServiceCollection services ,IConfiguration configuration)
        {
            var DbStr = configuration.Get<IdentitySQLSetting>();
            if(DbStr.ConnectionSqlSetting == string.Empty)
            {
                throw new ArgumentNullException("没有配置相关连接字符串");
            }
            services.AddDbContext<Notcomd_Identity_DbContext>(opt => opt.UseNpgsql(DbStr.ConnectionSqlSetting));
            services.AddDbContext<Notcomd_Append_DbContext>(opt => opt.UseNpgsql(DbStr.ConnectionSqlSetting));
        }
    }
}
