namespace Identity.Domain.IService;

public interface IGitHubAuthService
{
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    string GithubIndexAsync(string clientId);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="code"></param>
    /// <returns></returns>
    Task<string> RedirectUriAsync(string code);


    /// <summary>
    ///
    /// </summary>
    /// <returns></returns>
    Task<bool> BindLinkUserAsync();
}