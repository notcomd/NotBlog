namespace Identity.Domain.IRepository;

public interface IUserRepository
{
    ValueTask<User?> FindOneByUserAsync(Guid guid);

    ValueTask<User?> FindOneByUserAsync(PhoneNumber phoneNumber);

    ValueTask<User?> FindOneByUserAsync(string email);

    ValueTask AddOneByUserAsync(User user);

    ValueTask AddByLoginHistoryAsync(PhoneNumber phoneNumber, string message);

    ValueTask SaveByPhoneNumberAsync(PhoneNumber phoneNumber, string code);

    ValueTask<string> RetirievePhoneCodeAsync(PhoneNumber phoneNumber);

    ValueTask<string> FindPhoneNumberAsync(PhoneNumber phoneNumber);

    ValueTask SaveByEmailNumberAsync(string email, string code);
}