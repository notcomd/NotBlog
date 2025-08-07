namespace Identity.Domain.IRepository;

public interface IUserRepository : IRepository<User>
{


    ValueTask<User?> FindOneByUserAsync(Guid guid);

    ValueTask<User?> FindOneByUserAsync(PhoneNumber phoneNumber);

    ValueTask<User?> FindOneByUserAsync(string email);

    ValueTask AddOneByUserAsync(User user);

    ValueTask UpdateByUserAsync(User user);

    ValueTask UpdateByUserSafety(UserSafety userSafety);

   //ValueTask UpdateByUserClaim(UserClaim claim);


}