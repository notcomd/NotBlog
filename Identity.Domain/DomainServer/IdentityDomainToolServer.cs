

using Identity.Domain.IdentiyResult;


namespace Identity.Domain.DomainServer;

public class IdentityDomainToolServer
{

    private readonly INotDateTime _notDateTime;
    private readonly ILogger<IdentityDomainToolServer> _logger;
    private readonly IUserRepository _userRepository;
    private readonly INotMemoryCache _memoryCache;
    private readonly IJwtTokenService _jwtTokenService;


    public IdentityDomainToolServer(INotDateTime notDateTime, ILogger<IdentityDomainToolServer> logger,
        IUserRepository userRepository, INotMemoryCache memoryCache, IJwtTokenService jwtTokenService)
    {
        _notDateTime = notDateTime ?? throw new ArgumentNullException(nameof(notDateTime));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        _jwtTokenService = jwtTokenService;
    }



    public async ValueTask<UserAccessResult> GenerateWithCodeAsync(string memoryKey, int generateLegth)
    {
        if (string.IsNullOrEmpty(memoryKey))
            throw new ArgumentNullException(nameof(memoryKey));
        var code = await GenerateHelper.CreateRandomStringValueTask(generateLegth);
        if (string.IsNullOrEmpty(code))
            throw new InvalidOperationException("Generated code is null or empty.");
        if (await _memoryCache.IsExistsAsync(memoryKey))
            return UserAccessResult.AlreadyExists;
        await _memoryCache.AddByMemoryCacheAsync(memoryKey, code, 10);
        return UserAccessResult.Success;
    }



    public async ValueTask<string> GetCodeByMemoryCacheAsync(string memoryKey)
    {
        if (string.IsNullOrEmpty(memoryKey))
            throw new ArgumentNullException(nameof(memoryKey));
        var code = await _memoryCache.GetByMemoryCacheAsync(memoryKey);
        if (string.IsNullOrEmpty(code))
            throw new InvalidOperationException("Code not found in memory cache.");
        return code;
    }

    /// <summary>
    ///  检验是否已存在验证码。
    /// </summary>
    /// <param name="memoryKey"></param>
    /// <returns></returns>
    public async ValueTask<bool> IsCheckWithAlreadyExistsAsync(string memoryKey)
    {
        return await _memoryCache.IsExistsAsync(memoryKey);
    }


    public async ValueTask<string> BuilderWithAuthorToknAsync(IEnumerable<Claim> claims)
    {
        try
        {
            var userIdClaim = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                _logger.LogWarning($"[(≧ ﹏ ≦){_notDateTime.UtcNow}] Claim中缺少用户ID信息。");
                throw new ArgumentException("Claim中缺少用户ID信息。");
            }
            var userId = userIdClaim.Value;
            var user = await _userRepository.FindOneByUserAsync(Guid.Parse(userId));
            if (user is null)
            {
                _logger.LogWarning($"[(≧ ﹏ ≦){_notDateTime.UtcNow}] 用户 {userId} 不存在。");
                throw new ArgumentException("用户不存在。");
            }
            var token = await _jwtTokenService.BuilderTokenAsync(claims);

            return token;

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[ (≧ ﹏ ≦) {_notDateTime.UtcNow}]在生成Token时出现问题!");
            throw;
        }

    }

    public async ValueTask<bool> IsCheckWithVerifyGenerateCodeAsync(string memoryKey, string code)
    {
        if (string.IsNullOrEmpty(memoryKey))
            throw new ArgumentNullException(nameof(memoryKey));
        if (string.IsNullOrEmpty(code))
            throw new ArgumentNullException(nameof(code));
        return await _memoryCache.IsValidateCodeAsync(memoryKey, code);
    }

}
