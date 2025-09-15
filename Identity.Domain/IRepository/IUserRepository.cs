using Microsoft.AspNetCore.Identity;

namespace Identity.Domain.IRepository;

public interface IUserRepository : IRepository<User>
{


    ValueTask<User?> FindOneByUserAsync(Guid guid);

    ValueTask<User?> FindOneByUserAsync(PhoneNumber phoneNumber);

    ValueTask<User?> FindOneByUserAsync(string email);

    //ValueTask<IEnumerable<User>> FindOneByUserAsync(string email, string phoneNumber);

    ValueTask<IEnumerable<User>> FindAllByUserAsync();

    ValueTask AddOneByUserClaimAsync(UserClaim userClaim);

    ValueTask AddOneByUserAsync(User user);


    ValueTask UpdateByUserAsync(User user);

    ValueTask UpdateByUserSafetyAsync(UserSafety userSafety);

    ValueTask <IEnumerable<UserClaim>?> FindUserClaimsByUserAsync(Guid userGuid);

    ValueTask UpdateByUserClaimAsync(Guid userGuid,Func<User,Task> userAction);

    ValueTask UpdateByUserSafetyAsync(string findEmail, Func<User,Task> userSafetyAction);

    ValueTask UpdateByUserAsync(string userEmail, Func<User, Task> userAction);

}