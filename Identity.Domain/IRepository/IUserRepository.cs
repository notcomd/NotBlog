namespace Identity.Domain.IRepository;

public interface IUserRepository : IRepository<User>
{


    ValueTask<User?> FindOneByUserAsync(Guid guid);

    ValueTask<User?> FindOneByPhoneUserAsync(PhoneNumber phoneNumber);

    ValueTask<User?> FindOneByEmailUserAsync(string email);

    ValueTask AddOneByUserAsync(User user);

    ValueTask UpdateByUserAsync(User user);


    //ValueTask AddByLoginHistoryAsync(PhoneNumber phoneNumber, string message);


}