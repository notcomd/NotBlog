using System.Diagnostics.CodeAnalysis;
using EmailSendServer;
using MailKit.Security;
using Microsoft.AspNetCore.Mvc;
using MimeKit;
using Notcomd.Token.JWT;

namespace Identity.Web.API.Controllers;

[Route("LogIn/[Controller]")]
[ApiController]
public class UserLoginController : ControllerBase
{
    private readonly IEmail _email;
    private readonly ILogger<IEmail> _logger;

    public UserLoginController(IEmail email, ILogger<IEmail> logger)
    {
        _email = email;
        _logger = logger;
    }

    [HttpGet("TestSendEmail")]
    public async Task<ActionResult<string>> SendEmailAsync()
    {
        var mailpush = new MailPush("hello", "notcomd@outlook.com", "notcomd@outlook.com");

        mailpush.SendEmailAddresses.Add(new MailboxAddress
        (
            "1111", "2961767846@qq.com"
        ));
        await ValueTask.CompletedTask;
        var message = new MimeMessage
        {
            Subject = "hello",
            Body = new BodyBuilder
            {
                HtmlBody =
                    "<dir style=\"background-color: deepskyblue; width: auto; height: 60px;\">\n    <span style=\"text-align: left;\"><h1>Notcomd Studio</h1></span>\n</dir>\n<dir style=\" width: auto; height: max-content;\">\n    <span style=\"text-align: center;\"><h1>验证码</h1></span>\n    <span style=\"text-align:center;\"><h2>345345</h2></span>\n</dir>"
            }.ToMessageBody()
        };
        await _email.SendEmailValueTask(message, mailpush, SecureSocketOptions.StartTls);
        return new ActionResult<string>("ok");
    }

    [HttpGet("NuFection")]
    public ActionResult<string> PushTest()
    {
        return new ActionResult<string>("这个接口不作任何事同时也没有任何业务逻辑");
    }

    [HttpGet("UNRandom")]
    public async Task<ActionResult<long>> GetRandom()
    {
        return new ActionResult<long>(await JwtRandom.CreateRandomValueTask());
    }
}