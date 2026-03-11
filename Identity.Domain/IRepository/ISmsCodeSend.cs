namespace Identity.Domain.IRepository;

public interface ISmsCodeSend
{
    /// <summary>
    /// </summary>
    /// <param name="phoneNumber">
    /// </param>
    /// <param name="code"></param>
    /// <returns>
    /// </returns>
    ValueTask SendPhoneCodeAsync(PhoneNumber phoneNumber, string code);
}