using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Identity.Infrastructure.Repository;

/// <summary>
/// 短信验证码发送（F-07）。
/// 默认未接入短信服务商：由配置开关 SmsCode:Enabled 控制，关闭时显式降级（返回"未启用"），不抛异常。
/// </summary>
public class SmsCodeSend(IConfiguration configuration, ILogger<SmsCodeSend> logger) : ISmsCodeSend
{
    public ValueTask SendPhoneCodeAsync(PhoneNumber phoneNumber, string code)
    {
        if (!configuration.GetValue<bool>("SmsCode:Enabled"))
        {
            logger.LogInformation(
                "[SmsCodeSend] 短信验证码未启用（SmsCode:Enabled=false），跳过发送: {PhoneCode}",
                phoneNumber.PhoneCode);
            return ValueTask.CompletedTask;
        }

        // TODO(F-07): 接入短信服务商（如阿里云/腾讯云 SMS）后在此实现真实发送
        throw new NotSupportedException("短信服务尚未接入：请配置短信服务商后实现发送逻辑");
    }
}