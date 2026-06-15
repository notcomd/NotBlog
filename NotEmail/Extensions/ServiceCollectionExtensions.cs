using Microsoft.Extensions.DependencyInjection;
using Notcomd.NotEmail.Core;
using Notcomd.NotEmail.Providers;
using Notcomd.NotEmail.Services;
using Notcomd.NotEmail.Templates;

namespace Notcomd.NotEmail.Extensions;

/// <summary>
/// NotEmail DI 注册扩展
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 注册邮件发送服务（SMTP）
    /// </summary>
    public static IServiceCollection AddNotEmail(this IServiceCollection services, Action<EmailOptions> configure)
    {
        services.Configure(configure);
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddScoped<IEmailManager, EmailManager>();
        return services;
    }

    /// <summary>
    /// 注册邮件发送 + IMAP 接收服务
    /// </summary>
    public static IServiceCollection AddNotEmailWithImap(this IServiceCollection services,
        Action<EmailOptions> configure)
    {
        services.Configure(configure);
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddScoped<IEmailReceiver, ImapEmailReceiver>();
        services.AddScoped<IEmailManager, EmailManager>();
        return services;
    }

    /// <summary>
    /// 使用预置服务商配置注册（推荐）
    /// 
    /// 用法:
    ///   services.AddNotEmailByProvider(EmailProviderType.Google, "user@gmail.com", "app-password");
    /// </summary>
    public static IServiceCollection AddNotEmailByProvider(this IServiceCollection services,
        EmailProviderType provider, string email, string password, bool enableImap = true)
    {
        var options = EmailProviderConfig.FromProvider(provider, email, password);

        if (!enableImap)
            options.EnableImap = false;

        // 直接使用实例配置
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        if (enableImap)
            services.AddScoped<IEmailReceiver, ImapEmailReceiver>();
        services.AddScoped<IEmailManager, EmailManager>();

        // 用预填充的选项配置
        services.Configure<EmailOptions>(opt =>
        {
            opt.SmtpHost = options.SmtpHost;
            opt.SmtpPort = options.SmtpPort;
            opt.ImapHost = options.ImapHost;
            opt.ImapPort = options.ImapPort;
            opt.FromEmail = options.FromEmail;
            opt.FromName = options.FromName;
            opt.Password = options.Password;
            opt.UseSsl = options.UseSsl;
            opt.EnableImap = options.EnableImap;

            // OAuth 2.0 属性
            opt.UseOAuth2 = options.UseOAuth2;
            opt.ClientId = options.ClientId;
            opt.ClientSecret = options.ClientSecret;
            opt.TenantId = options.TenantId;
            opt.AuthorizationEndpoint = options.AuthorizationEndpoint;
            opt.TokenEndpoint = options.TokenEndpoint;
            opt.OAuthScopes = options.OAuthScopes;
            opt.AccessTokenCallback = options.AccessTokenCallback;
        });

        return services;
    }

    /// <summary>
    /// 注册邮件 + OAuth 2.0 认证
    /// 
    /// 简化 OAuth 2.0 注册，适合通过 appsettings 配置。
    /// 需要在 appsettings.json 中设置相应的 OAuth 配置节。
    /// 
    /// 用法:
    ///   services.AddNotEmailWithOAuth(opt =>
    ///   {
    ///       opt.UseOAuth2 = true;
    ///       opt.ClientId = builder.Configuration["Email:ClientId"]!;
    ///       opt.ClientSecret = builder.Configuration["Email:ClientSecret"]!;
    ///       opt.AccessTokenCallback = async ct => await tokenService.GetAccessTokenAsync();
    ///   });
    /// </summary>
    public static IServiceCollection AddNotEmailWithOAuth(this IServiceCollection services,
        Action<EmailOptions> configure, bool enableImap = false)
    {
        services.Configure(configure);
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        if (enableImap)
            services.AddScoped<IEmailReceiver, ImapEmailReceiver>();
        services.AddScoped<IEmailManager, EmailManager>();
        return services;
    }

    /// <summary>
    /// 使用 OAuth 2.0 预置配置 + IMAP 注册
    /// 
    /// 用法:
    ///   services.AddNotEmailWithOAuth2(
    ///       opt => EmailProviderConfig.GoogleOAuth2(clientId, clientSecret, tokenCallback));
    /// </summary>
    public static IServiceCollection AddNotEmailWithOAuth2Provider(this IServiceCollection services,
        Func<EmailOptions> optionsFactory, bool enableImap = true)
    {
        var options = optionsFactory();
        services.Configure<EmailOptions>(opt =>
        {
            opt.SmtpHost = options.SmtpHost;
            opt.SmtpPort = options.SmtpPort;
            opt.ImapHost = options.ImapHost;
            opt.ImapPort = options.ImapPort;
            opt.FromEmail = options.FromEmail;
            opt.FromName = options.FromName;
            opt.Password = options.Password;
            opt.UseSsl = options.UseSsl;
            opt.EnableImap = enableImap;
            opt.UseOAuth2 = options.UseOAuth2;
            opt.ClientId = options.ClientId;
            opt.ClientSecret = options.ClientSecret;
            opt.TenantId = options.TenantId;
            opt.AuthorizationEndpoint = options.AuthorizationEndpoint;
            opt.TokenEndpoint = options.TokenEndpoint;
            opt.OAuthScopes = options.OAuthScopes;
            opt.AccessTokenCallback = options.AccessTokenCallback;
        });

        services.AddScoped<IEmailSender, SmtpEmailSender>();
        if (enableImap)
            services.AddScoped<IEmailReceiver, ImapEmailReceiver>();
        services.AddScoped<IEmailManager, EmailManager>();
        return services;
    }

    /// <summary>
    /// 注册邮件模板系统
    /// </summary>
    public static IServiceCollection AddNotEmailTemplates(this IServiceCollection services,
        Action<TemplateService> configure)
    {
        services.AddSingleton<ITemplateEngine, ScribanTemplateEngine>();
        services.AddSingleton(sp =>
        {
            var engine = sp.GetRequiredService<ITemplateEngine>();
            var templateService = new TemplateService(engine);
            configure?.Invoke(templateService);
            return templateService;
        });
        return services;
    }
}