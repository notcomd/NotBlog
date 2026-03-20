using Identity.Domain.Dto.OAuth;

namespace Identity.Domain.IService;

public interface IOAuthService
{
    Task<string> GenerateAuthorizationUrlAsync(string provider, string redirectUri);
    
    Task<OAuthLoginResponse> HandleCallbackAsync(string provider, string code, string redirectUri);
    
    Task<User?> GetExistingUserByExternalLoginAsync(string provider, string providerUserId);
    
    Task<User> CreateOrUpdateUserFromExternalLoginAsync(string provider, ExternalUserInfo externalUserInfo);
    
    Task LinkExternalLoginToUserAsync(Guid userId, string provider, string providerUserId, string displayName);
}
