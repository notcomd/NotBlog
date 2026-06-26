using Identity.Domain.Entities.UserExternalLoginAggregate;

namespace Identity.Domain.IService;

public interface IUserExternalLoginService
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="userExternalLogin"></param>
    /// <returns></returns>
    public Task CrateByUserExternalLoginAsync(UserExternalLogin userExternalLogin);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="findKey"></param>
    /// <returns></returns>
    public Task FindByUserExternalLoginAsync(string findKey);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="ProviderKey"></param>
    /// <returns></returns>
    public Task DeleteByUserExternalLoginAsync(string ProviderKey);


    /// <summary>
    /// 
    /// </summary>
    /// <param name="userExternalLogin"></param>
    /// <returns></returns>
    public Task UpdateByUserExternalLoginAsync(UserExternalLogin userExternalLogin);
}