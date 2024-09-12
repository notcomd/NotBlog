using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MimeKit;
using EmailSendServer;

namespace MyServerTest;

public class WorkAsync
{
    
    private readonly IServiceScope _serviceScope;
    private readonly ILogger _logger;
    private readonly IEmail _email;

    public WorkAsync(){}
    public WorkAsync(IEmail email ,ILogger logger, IServiceScopeFactory serviceScopeFactory)
    {
        this._logger = logger;
        _email = email;
        this._serviceScope = serviceScopeFactory.CreateAsyncScope();
    }
    
    public  ValueTask WorkValueTask(CancellationToken cancellationToken)
    {
        IConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddJsonFile("appsettings.json", false, true);
        var config = configurationBuilder.Build();

        var build = new ServiceCollection();
        build.AddOptions().Configure<EmailAddress>(en => config.GetSection("EmailAddress"));
        build.AddScoped<IEmail, Email>();
        var mailpush = new MailPush("hello", "notcomd@outlook.com", "notcomd@outlook.com");
            
        mailpush.AddPushValueTask(new MailboxAddress
        (
            "1111","2961767846@qq.com"
        ));
        //var provider = build.BuildServiceProvider().GetRequiredService<IEmail>();
      
        var message = new MimeMessage
        {
            Subject = "hello",
            Body = new BodyBuilder
            {   
                HtmlBody = $"我是时间{DateTime.Now}"
            }.ToMessageBody()
        };
        _email.SendEmailValueTask(message, mailpush);
        return ValueTask.CompletedTask;
        
    }
}