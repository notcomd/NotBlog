using Microsoft.Identity.Client;

namespace Identity.Web.API.Extensions;

/// <summary>
/// Outlook / Office 365 OAuth 2.0 Access Token 获取服务（MSAL 公共客户端）
///
/// 认证流程:
///   1. 首次运行使用"设备代码流"，在浏览器授权一次
///   2. 之后优先使用缓存/静默续期（AcquireTokenSilent），无需重复授权
///
/// 前置条件（Azure Portal）:
///   - 应用注册 → 支持的账户类型选择对应的个人/组织账号
///   - 认证 → 添加"移动和桌面应用程序"平台（公共客户端）
///   - API 权限 → 添加 Microsoft Graph 委托权限 SMTP.Send（+ offline_access）
/// </summary>
public class OutlookTokenService
{
    private readonly IPublicClientApplication _app;
    private readonly string[] _scopes = { "https://outlook.office.com/SMTP.Send" };
    private readonly string _cacheFilePath;
    private AuthenticationResult? _cached;
    private Task<AuthenticationResult>? _pendingAuthTask;

    /// <param name="cachePath">token 缓存文件路径；为空时默认 %LOCALAPPDATA%\NotBlog\outlook-msal-cache.bin</param>
    public OutlookTokenService(string clientId, string tenantId = "consumers", string? cachePath = null)
    {
        // token 缓存落盘：授权一次后，应用重启也无需重复授权；路径可配置
        _cacheFilePath = cachePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NotBlog", "outlook-msal-cache.bin");

        _app = PublicClientApplicationBuilder.Create(clientId)
            .WithAuthority(new Uri($"https://login.microsoftonline.com/{tenantId}"))
            .Build();

        RegisterCacheSerialization();
    }

    /// <summary>
    /// 触发设备代码流，立即返回授权链接与代码（供管理端点展示，不依赖控制台）。
    /// 用户完成浏览器授权后，token 自动写入缓存，后续经 GetAccessTokenAsync 静默获取。
    /// </summary>
    public async Task<DeviceCodeResult> BeginDeviceCodeAsync(CancellationToken ct = default)
    {
        var tcs = new TaskCompletionSource<DeviceCodeResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingAuthTask = _app.AcquireTokenWithDeviceCode(_scopes, code =>
        {
            tcs.TrySetResult(code);
            return Task.CompletedTask;
        }).ExecuteAsync(ct);

        // 设备码回调几乎立即触发；若设备码申请本身失败则抛出异常
        var first = await Task.WhenAny(tcs.Task, _pendingAuthTask).WaitAsync(ct);
        if (first == _pendingAuthTask)
            await _pendingAuthTask;
        return tcs.Task.Result;
    }

    /// <summary>
    /// 将 MSAL token 缓存序列化到本地文件，实现跨重启的静默续期
    /// </summary>
    private void RegisterCacheSerialization()
    {
        _app.UserTokenCache.SetBeforeAccess(args =>
        {
            args.TokenCache.DeserializeMsalV3(
                File.Exists(_cacheFilePath) ? File.ReadAllBytes(_cacheFilePath) : null);
        });
        _app.UserTokenCache.SetBeforeWrite(args =>
        {
            args.TokenCache.DeserializeMsalV3(
                File.Exists(_cacheFilePath) ? File.ReadAllBytes(_cacheFilePath) : null);
        });
        _app.UserTokenCache.SetAfterAccess(args =>
        {
            if (args.HasStateChanged)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_cacheFilePath)!);
                File.WriteAllBytes(_cacheFilePath, args.TokenCache.SerializeMsalV3());
            }
        });
    }

    /// <summary>
    /// 获取（必要时刷新）Access Token
    /// </summary>
    public async Task<string> GetAccessTokenAsync(CancellationToken ct = default)
    {
        // 有正在等待授权的设备代码流（管理端点触发）→ 等待其完成
        if (_pendingAuthTask is not null)
        {
            _cached = await _pendingAuthTask;
            _pendingAuthTask = null;
            return _cached.AccessToken;
        }

        // 缓存未过期则直接复用，避免每次发信都走网络
        if (_cached is not null && _cached.ExpiresOn > DateTimeOffset.UtcNow.AddMinutes(5))
            return _cached.AccessToken;

        var accounts = await _app.GetAccountsAsync();
        if (accounts.Any())
        {
            try
            {
                _cached = await _app.AcquireTokenSilent(_scopes, accounts.First()).ExecuteAsync(ct);
                return _cached.AccessToken;
            }
            catch (MsalUiRequiredException)
            {
                // 无有效刷新令牌（首次运行或令牌已失效），回退到设备代码流
            }
        }

        _cached = await _app.AcquireTokenWithDeviceCode(_scopes, code =>
        {
            Console.WriteLine(
                $"[NotEmail] 请在浏览器打开 {code.VerificationUrl} 并输入代码 {code.UserCode} 完成 Outlook 授权");
            return Task.CompletedTask;
        }).ExecuteAsync(ct);

        return _cached.AccessToken;
    }
}
