using MailKit.Security;
using MimeKit;

namespace Identity.Web.API.Application.CommandHandler;

public class CreateByUserCommandHandler : IRequestHandler<CreateByUserCommand, bool>
{

    private readonly IEmail _email;
    private readonly ILogger<CreateByUserCommandHandler> _logger;


    public CreateByUserCommandHandler(IEmail email, ILogger<CreateByUserCommandHandler> logger)
    {
        _email = email ?? throw new ArgumentNullException(nameof(email));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    }

    public async Task<bool> Handler(CreateByUserCommand request, CancellationToken cancellationToken)
    {
        var emailPush = new MailPush("测试用例", request.Email);

        var message = new MimeMessage
        {
            Subject = emailPush.TitleEmail,
            Body = new BodyBuilder
            {
                HtmlBody =
                    "<!DOCTYPE html>\n<html>\n<head>\n    <meta charset=\"utf-8\">\n    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">\n    <title>Email Template</title>\n</head>\n<body style=\"margin: 0; padding: 0; font-family: Arial, sans-serif; line-height: 1.6;\">\n    <!-- 外层容器 -->\n    <table width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"max-width: 600px; margin: 20px auto; background-color: #ffffff;\">\n        <!-- 头部 -->\n        <tr>\n            <td style=\"padding: 20px 30px; border-bottom: 1px solid #eeeeee;\">\n                <a href=\"#\" style=\"text-decoration: none; color: #333333;\">\n                    <img src=\"logo-placeholder.png\" alt=\"Notcomd\" width=\"120\" style=\"display: block;\">\n                </a>\n            </td>\n        </tr>\n\n        <!-- 正文内容 -->\n        <tr>\n            <td style=\"padding: 30px;\">\n                <h1 style=\"font-size: 24px; color: #333333; margin-top: 0;\">尊敬的[姓名]：</h1>\n                \n                <p style=\"color: #666666; margin: 15px 0;\">\n                    感谢您的来信。我们已收到您的咨询，将会在24小时内给您回复。\n                </p>\n\n                <p style=\"color: #666666; margin: 15px 0;\">\n                    如需即时帮助，请联系我们的客服团队：<br>\n                    \ud83d\udcde 电话：400-123-4567<br>\n                    \ud83d\udce7 邮箱：support@company.com\n                </p>\n            </td>\n        </tr>\n\n        <!-- 尾部 -->\n        <tr>\n            <td style=\"padding: 20px 30px; background-color: #f8f9fa; color: #666666; font-size: 12px;\">\n                <p style=\"margin: 5px 0;\">\n                    \u00a9 2023 公司名称 | 地址信息\n                </p>\n                <p style=\"margin: 5px 0;\">\n                    <a href=\"#\" style=\"color: #0066cc; text-decoration: none;\">隐私政策</a> | \n                    <a href=\"#\" style=\"color: #0066cc; text-decoration: none;\">退订邮件</a>\n                </p>\n            </td>\n        </tr>\n    </table>\n</body>\n</html>\n"

            }.ToMessageBody()
        };
        await _email.SendEmailValueTask(message, emailPush, SecureSocketOptions.SslOnConnect);
        _logger.LogInformation($"[{DateTime.UtcNow}]Email Send! ");
        return true;
    }
}