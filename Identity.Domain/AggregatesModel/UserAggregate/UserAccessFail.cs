using Identity.Domain.Events;

namespace Identity.Domain.AggregatesModel.UserAggregate;

public class UserAccessFail : Entity
{
  
    public Guid UserGuid { get; init; }

    public DateTimeOffset? LockOutEnd { get; private set; }

    public int AccessFaildCount { get; private set; }

    private bool IsLockOut => LockOutEnd.HasValue && LockOutEnd.Value > DateTimeOffset.UtcNow;


    protected UserAccessFail()
    {
        Id = Guid.CreateVersion7();
    }

    public static ValueTask<UserAccessFail> CreateByUserAccessFailAsync(Guid userGuid)
    {
        return new ValueTask<UserAccessFail>(CreateByUserAccessFail(userGuid));
    }

    public UserAccessFail(User user)
    {
        var userAccessFail = new UserAccessFail
        {
            UserGuid = user.Id,
            LockOutEnd = null,
            AccessFaildCount = 0,
        };       
    }

    public static  UserAccessFail CreateByUserAccessFail(Guid userGuid)
    {
        if (userGuid == Guid.Empty)
        {
            Console.WriteLine($"debug Guid =>{userGuid}");
            throw new ArgumentNullException(nameof(userGuid), "UserGuid cannot be empty");
        }
        var userAccessFail = new UserAccessFail
        {
            UserGuid = userGuid,
            LockOutEnd = null,
            AccessFaildCount = 0,
        };
        return userAccessFail;

    }

    public void VerifyByAccessFailed()
    {
        AccessFaildCount++;
        if (AccessFaildCount > 5)
        {
            LockOutEnd = DateTimeOffset.UtcNow.AddMinutes(15);
            AddDomainEvent(new AccountLockedDomainEvent(UserGuid, LockOutEnd));
        }
    }

    public bool IsLockOutByAccessFaild() =>
        IsLockOut;

    public void ResetFail()
    {
        if (!IsLockOut)
        {
            LockOutEnd = null;
            AccessFaildCount = 0;           
        }
    }

}