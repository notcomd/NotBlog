namespace Identity.Domain.IRepository;

public interface ISmsCodeSend
{
    /// <summary>
    /// 发送短信验证码
    /// </summary>
    /// <param name="phoneNumber">手机号</param>
    /// <param name="code">验证码</param>
    /// <returns>任务</returns>
    ValueTask SendPhoneCodeAsync(PhoneNumber phoneNumber, string code);
}