namespace Identity.Domain.IRepository;

public interface IUserRepository1 : IRepository<User>
{
    ValueTask AddByLoginHistoryAsync(PhoneNumber phoneNumber, string message);

    ValueTask AddOneByUserAsync(User user);

    ValueTask<User?> FindOneByUserAsync(Guid guid);

    ValueTask<User?> FindOneByUserAsync(PhoneNumber phoneNumber);

    ValueTask<User?> FindOneByUserAsync(string email);

    ValueTask<string> FindPhoneNumberAsync(PhoneNumber phoneNumber);

    ValueTask<string> RetirievePhoneCodeAsync(PhoneNumber phoneNumber);

    ValueTask SaveByEmailNumberAsync(string email, string code);

    ValueTask SaveByPhoneNumberAsync(PhoneNumber phoneNumber, string code);
}