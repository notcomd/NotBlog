using System.Reflection;

using EmailSendServer;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Notcomd.DomainCommand;
using Notcomd.Evenbus;
using Notcomd.Token.JWT;

namespace CommonsInitializer;

public static class WebApplicationBuilderExtension
{


    public static void NotBlogConfigureExtraServices(this WebApplicationBuilder builder, InitializerOptions initOptions)
    {
        var services = builder.Services;
        IConfiguration configuration = builder.Configuration;
        var assemblies = ReflectionHelper.GetAllReferencedAssemblies();
        var enumerable = assemblies as Assembly[] ?? assemblies.ToArray();
        services.AddAutoAddInstance(enumerable);
        services.AddAllDbContexts(ctx =>
        {
            //连接字符串如果放到appsettings.json中，会有泄密的风险
            //如果放到UserSecrets中，每个项目都要配置，很麻烦
            //因此这里推荐放到环境变量中。
            var connStr = configuration.GetSection(nameof(ConnectionDbContextOptions)).Get<ConnectionDbContextOptions>();
            if (connStr is null) throw new ArgumentNullException("没有配置ConnectionDbContextOptions");
            ctx.UseNpgsql(connStr.DbConnectionString);
        }, enumerable);








        //开始:Authentication,Authorization
        //只要需要校验Authentication报文头的地方（非IdentityService.WebAPI项目）也需要启用这些
        //IdentityService项目还需要启用AddIdentityCore
        builder.Services.AddAuthorization();
        // builder.Services.AddAuthentication();
        builder.Services.AddAuthenticationCore();
        var jwtOptions = configuration.GetSection(nameof(JwtConfigurationOptions)).Get<JwtConfigurationOptions>() ?? throw new ArgumentNullException($"没有配置JwtOptions{nameof(JwtConfigurationOptions)}");
        builder.Services.AddJwtAuthentication(jwtOptions);


        //启用Swagger中的【Authorize】按钮。这样就不用每个项目的AddSwaggerGen中单独配置了
        // builder.Services.Configure<SwaggerGenOptions>(c =>
        //{
        //               c.AddAuthenticationHeader();
        //         });
        //结束:Authentication,Authorization

        services.AddMediator(enumerable);

        //现在不用手动AddMVC了，因此把文档中的services.AddMvc(options =>{})改写成Configure<MvcOptions>(options=> {})这个问题很多都类似
        services.Configure<MvcOptions>(options =>
        {
            options.Filters.Add<UnitOfWorkFilter>();
        });




        // services.Configure<JsonOptions>(options =>
        // {
        //设置时间格式。而非“2008-08-08T08:08:08”这样的格式
        //   options.JsonSerializerOptions.Converters.Add(new DateTimeJsonConverter("yyyy-MM-dd HH:mm:ss"));
        // });

        ///配置跨域请求
        services.AddCors(options =>
            {
                //更好的在Program.cs中用绑定方式读取配置的方法：https://github.com/dotnet/aspnetcore/issues/21491
                //不过比较麻烦。
                var corsOpt = configuration.GetSection(nameof(CorsSettings)).Get<CorsSettings>();
                string[] urls = corsOpt!.AllowedOrigins;
                options.AddDefaultPolicy(builder => builder.WithOrigins(urls)
                    .AllowAnyMethod().AllowAnyHeader().AllowCredentials());
            }
        );

        services.AddEmailServer(en =>
        {
            var getEmailOptions = configuration.GetSection(nameof(EmailConfigurationOptions)).Get<EmailConfigurationOptions>();
            if (getEmailOptions is null) throw new ArgumentNullException("没有配置EmailConfiguration");
            en.OptionSsL = getEmailOptions.OptionSsL;
            en.Port = getEmailOptions.Port;
            en.SmtpHost = getEmailOptions.SmtpHost;
            en.FromEmail = getEmailOptions.FromEmail;
            en.SmtpPassword = getEmailOptions.SmtpPassword;
        });


        //services.AddLogging(builder =>
        //{
        //    Log.Logger = new LoggerConfiguration()
        // .MinimumLevel.Information().Enrich.FromLogContext()
        //      .WriteTo.Console()
        //     .WriteTo.File(initOptions.LogFilePath)
        //     .CreateLogger();
        //  builder.AddSerilog();
        //});
        // services.AddFluentValidation(fv =>
        //{                
        // fv.RegisterValidatorsFromAssemblies(assemblies);
        // });

        //services.Configure<JwtConfigurationOptions>(configuration.GetSection("PrivateKey"));
        services.Configure<IntegrationEventRabbitMQOptions>(configuration.GetSection(nameof(IntegrationEventRabbitMQOptions)));
        services.AddEventBus(initOptions.EventBusQueueName, enumerable);

        //Redis的配置
        //string redisConnStr = configuration.GetValue<string>("Redis:ConnStr");
        //IConnectionMultiplexer redisConnMultiplexer = ConnectionMultiplexer.Connect(redisConnStr);
        //services.AddSingleton(typeof(IConnectionMultiplexer), redisConnMultiplexer);
        //services.Configure<ForwardedHeadersOptions>(options =>
        //{
        //options.ForwardedHeaders = ForwardedHeaders.All;
        //});
    }
}