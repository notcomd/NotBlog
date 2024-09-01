using Identity.Domain.Entities;
using Identity.Domain.IRepository;
using Identity.Infrastructure.EntityConfig;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace Identity.Infrastructure.Repository;

public class UserRepository:IUserRepository
{

    private readonly UserDdContext _userDdContext;
    private readonly IDistributedCache _distributedCache;
    public UserRepository(UserDdContext userDdContext,IDistributedCache distributedCache)
    {
        _distributedCache = distributedCache;
        _userDdContext = userDdContext;
    }
    
    public ValueTask<User?> FindOneByUserAsync(Guid guid)
    {
        var data = _userDdContext.FindAsync<User>(guid).GetAwaiter().GetResult();
        return new ValueTask<User?>(data);
    }

    public ValueTask<User?> FindOneByUserAsync(PhoneNumber phoneNumber)
    {
        if (phoneNumber is null)
            throw new ArgumentNullException($"数据为空");
        //var data = _userDdContext.FindAsync<User>(phoneNumber).GetAwaiter().GetResult();
        var data = _userDdContext.Users.SingleOrDefaultAsync(en =>
                en.UserPhone!.PhoneCode == phoneNumber.PhoneCode &&
                en.UserPhone!.AddressRegion == phoneNumber.AddressRegion)
            .GetAwaiter().GetResult();
        //throw new NotImplementedException();
        return new ValueTask<User?>(data);
    }

    public ValueTask<User?> FindOneByUserAsync(string email)
    {
        var data = _userDdContext.FindAsync<User>(email).GetAwaiter().GetResult();
        return new ValueTask<User?>(data);
    }

    public ValueTask AddByLoginHistoryAsync(PhoneNumber phoneNumber, string message)
    {
        var find = FindOneByUserAsync(phoneNumber).GetAwaiter().GetResult();
        if (find is not null)
        {
            var userid = find.UserGuid;
            _userDdContext.FindAsync<User>(new UserLoginHistory(userid,phoneNumber,message));
        }
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// 保存验证马
    /// </summary>
    /// <param name="phoneNumber"></param>
    /// <param name="code"></param>
    /// <returns></returns>
    public ValueTask SaveByPhoneNumberAsync(PhoneNumber phoneNumber, string code)
    {
        string key = $"PhoneCode{phoneNumber.PhoneCode}_{phoneNumber.AddressRegion}";
        _distributedCache.SetStringAsync(key, code, new DistributedCacheEntryOptions {AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)});
        return ValueTask.CompletedTask;
    }

    public ValueTask SaveByEmailNumberAsync(string email, string code)
    {
        var data=_userDdContext.Users.SingleOrDefaultAsync(en => en.UserEmail == email).GetAwaiter().GetResult();
        if (data is null) return ValueTask.CompletedTask;
        string key = $"emailAddress:{data.UserEmail}_{code}";
        _distributedCache.SetStringAsync(key,code,new DistributedCacheEntryOptions{AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)});
        //string key = $"emailAddress:{data.UserEmail}_{code}";
        return ValueTask.CompletedTask;
    }
 
    public ValueTask<string> RetirievePhoneCodeAsync(PhoneNumber phoneNumber)
    {
        throw new NotImplementedException();
    }
    
    /// <summary>
    /// 验证过后直接删除
    /// </summary>
    /// <param name="phoneNumber"></param>
    /// <returns></returns>
    public ValueTask<string> FindPhoneNumberAsync(PhoneNumber phoneNumber)
    {
        var key = $"phoneCode{phoneNumber.PhoneCode},phoneAddressRegion{phoneNumber.AddressRegion}";
        var code = _distributedCache.GetStringAsync(key).GetAwaiter().GetResult()!;
        _distributedCache.RemoveAsync(key);
        return new ValueTask<string>(code);
    }
}