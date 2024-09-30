using Identity.Domain.Entities;

namespace Identity.Domain.IRepository;

public interface IBlackListRepository
{
    
    ValueTask<BlackList?> FindByBlackValueTask(PhoneNumber phoneNumber);
    
    ValueTask<BlackList?> FindByBlackValueTask(string email);

    ValueTask AddByBlackValueTask(BlackList blackList);

    ValueTask<bool> DeleteByBlackValueTask(Guid blackGuid);

    ValueTask<bool> UpdateByBlackValueTask(BlackList blackList);
    
}