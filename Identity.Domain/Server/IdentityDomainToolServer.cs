using Identity.Domain.IdentiyResult;
using Microsoft.Extensions.Caching.Memory;

namespace Identity.Domain.Server;

public class IdentityDomainToolServer
{

    private readonly INotDateTime.INotDateTime _notDateTime;
    private readonly ILogger<IdentityDomainToolServer> _logger;
    private readonly IUserRepository _userRepository;
    private readonly INotMemoryCache.INotMemoryCache _memoryCache;


    public IdentityDomainToolServer(INotDateTime.INotDateTime notDateTime, ILogger<IdentityDomainToolServer> logger,
        IUserRepository userRepository, INotMemoryCache.INotMemoryCache memoryCache)
    {
        _notDateTime = notDateTime?? throw new ArgumentNullException(nameof(notDateTime));
        _logger = logger?? throw new ArgumentNullException(nameof(logger));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
    }


    public async ValueTask<UserAccessResult> GenerateWithCodeAsync(string memoryKey,int codeLength)
    {
        if(string.IsNullOrEmpty(memoryKey))
            throw new ArgumentNullException(nameof(memoryKey));
        var code= await GenerateHelper.CreateRandomStringValueTask(codeLength);
        await _memoryCache.AddByMemoryCacheAsync(memoryKey,code);
        return UserAccessResult.Success;
    }

  

    public async ValueTask<string> GetCodeByMemoryCacheAsync(string memoryKey)
    {
        if(string.IsNullOrEmpty(memoryKey))
            throw new ArgumentNullException(nameof(memoryKey));
        var code = await _memoryCache.GetByMemoryCacheAsync(memoryKey);
        if (string.IsNullOrEmpty(code))
            throw new InvalidOperationException("Code not found in memory cache.");
        return code;
    }



    public async ValueTask<bool> IsCheckWithAlreadyExistsAsync(string memoryKey)
    {
        return await _memoryCache.IsExistsAsync(memoryKey);
    }

}
