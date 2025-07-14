namespace Identity.Infrastructure.Repository;

public class SmsCodeSend : ISmsCodeSend
{
    public ValueTask SendPhoneCodeAsync(PhoneNumber phoneNumber, string code)
    {
        throw new NotImplementedException();
    }
}