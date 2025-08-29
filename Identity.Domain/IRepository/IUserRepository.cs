namespace Identity.Domain.IRepository;

public interface IUserRepository : IRepository<User>
{


    ValueTask<User?> FindOneByUserAsync(Guid guid);

    ValueTask<User?> FindOneByUserAsync(PhoneNumber phoneNumber);

    ValueTask<User?> FindOneByUserAsync(string email);

    ValueTask AddOneByUserClaimAsync(UserClaim userClaim);

    ValueTask AddOneByUserAsync(User user);

    ValueTask UpdateByUserAsync(User user);

    ValueTask UpdateByUserSafetyAsync(UserSafety userSafety);

    ValueTask <IEnumerable<UserClaim>> FindUserClaimsByUserAsync(Guid userGuid);

    ValueTask UpdateByUserClaimAsync(Guid userGuid,Action<User> userAction);

    ValueTask UpdateByUserSafetyAsync(Guid guid, Action<UserSafety> userSafetyAction);

    ValueTask UpdateByUserAsync(string userEmail, Action<User> userAction);

}