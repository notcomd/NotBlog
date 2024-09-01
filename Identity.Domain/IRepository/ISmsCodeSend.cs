using Identity.Domain.Entities;

namespace Identity.Domain.IRepository;

public interface ISmsCodeSend
{
    ValueTask SendPhoneCodeAsync(PhoneNumber phoneNumber, string code);
}