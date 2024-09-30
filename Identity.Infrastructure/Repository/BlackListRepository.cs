using System.ComponentModel.DataAnnotations;
using Identity.Domain.Entities;
using Identity.Domain.IRepository;
using Identity.Infrastructure.EntityFramework;

namespace Identity.Infrastructure.Repository;

public class BlackListRepository : IBlackListRepository
{

    private readonly BlackListDbContext _blackListDbContext;

    public BlackListRepository(BlackListDbContext blackListDbContext)
    {
        _blackListDbContext = blackListDbContext;
    }
    
    public  ValueTask<BlackList?> FindByBlackValueTask(PhoneNumber phoneNumber)
    {
        var black = _blackListDbContext.BlackLists.Where(en =>
            en.PhoneNumber!.PhoneCode == phoneNumber.PhoneCode &&
            en!.PhoneNumber.AddressRegion == phoneNumber.AddressRegion)?.FirstOrDefault();
        return new ValueTask<BlackList?>(black);
    }

    
    public ValueTask<BlackList?> FindByBlackValueTask([EmailAddress]string email)
    {
        var black = _blackListDbContext.BlackLists.Where(en => en.Email == email)?.FirstOrDefault();
        return new ValueTask<BlackList?>(black);
    }

    
    public ValueTask AddByBlackValueTask(BlackList blackList)
    {
        throw new NotImplementedException();
    }

    
    public ValueTask<bool> DeleteByBlackValueTask(Guid blackGuid)
    {
        throw new NotImplementedException();
    }

    
    public ValueTask<bool> UpdateByBlackValueTask(BlackList blackList)
    {
        throw new NotImplementedException();
    }

    
    public ValueTask<bool> IsBlackValueTask()
    {

        return new ValueTask<bool>(true);
    }
}