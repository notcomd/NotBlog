using Microsoft.Extensions.DependencyInjection;
using MimeKit;
using Microsoft.Extensions.Configuration;
using EmailSendServer;

namespace MyServerTest;

[TestClass]
public class UnitTest1
{


    private readonly IServiceScope _serviceProvider;

    public UnitTest1(IServiceScopeFactory serviceProvider)
    {
        _serviceProvider = serviceProvider.CreateAsyncScope();
    }
    
    
    [TestMethod]
    public void TestMethod1()
    {
        
        // var configuration = new ConfigurationBuilder();
        //
        // configuration.AddJsonFile("appsettings.json",optional:false,true);
        //
        // var config = configuration.Build();
        //
        // var build = new ServiceCollection();        
        // //build.Configure<EmailAddress>(Configuration);
        // build.AddScoped<IEmail, Email>();
        //
        // build.AddOptions().Configure<EmailAddress>(en=>config.GetSection("EmailAddress"));
        //
        //
        // var mailpush = new MailPush("hello", "notcomd@outlook.com", "notcomd@outlook.com");
        //     
        //  mailpush.AddPushValueTask(new MailboxAddress
        // (
        //     "1111","2961767846@qq.com"
        // ));
        // //var provider = build.BuildServiceProvider().GetRequiredService<IEmail>();
        // var provider = _serviceProvider.ServiceProvider.GetRequiredService<IEmail>();
        // var message = new MimeMessage
        // {
        //     Subject = "hello",
        //     Body = new BodyBuilder
        //     {   
        //         HtmlBody = $"我是时间{DateTime.Now}"
        //     }.ToMessageBody()
        // };
        //
        // provider.SendEmailValueTask(message, mailpush);



        var work = new WorkAsync();
       
    }
}