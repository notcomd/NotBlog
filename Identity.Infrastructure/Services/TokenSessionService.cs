using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CacheMemory.Core;

namespace Identity.Infrastructure.Services;

public class TokenSessionService(
    IRedisCacheService redis,
    IJwtTokenService jwtTokenService,
    ILogger<TokenSessionService> logger) : ITokenSessionService
{
    /// <summary>单设备会话条目 key 前缀</summary>
    private const string SessionKeyPrefix = "auth:session:";

    /// <summary>用户会话指纹集合 key 前缀</summary>
    private const string UserSessionsKeyPrefix = "auth:user:";

    public async Task RegisterAsync(Guid userGuid, TokenResult tokenResult, TimeSpan accessTtl,
        TimeSpan refreshTtl, CancellationToken ct = default)
    {
        if (tokenResult is null || string.IsNullOrWhiteSpace(tokenResult.AccessToken))
            return;

        var fingerprint = Fingerprint(tokenResult.AccessToken);
        var entry = new TokenSessionEntry(
            userGuid, tokenResult.AccessToken, tokenResult.RefreshToken, tokenResult.ExpiresAt);

        // 条目 TTL 取 refresh 有效期（登出窗口覆盖 refresh 生命周期）
        var ttl = refreshTtl > accessTtl ? refreshTtl : accessTtl;

        await redis.StringSetAsync($"{SessionKeyPrefix}{fingerprint}",
            JsonSerializer.Serialize(entry), ttl, ct);
        // 用户会话集合用 Hash（field=指纹），接口层无原子 SetRemove，Hash 三件套齐全
        await redis.HashSetAsync($"{UserSessionsKeyPrefix}{userGuid}", fingerprint, "1", ct);
    }

    public async Task RevokeSessionAsync(string accessToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
            return;

        var fingerprint = Fingerprint(accessToken);
        var sessionKey = $"{SessionKeyPrefix}{fingerprint}";
        var json = await redis.StringGetAsync(sessionKey, ct);
        await redis.KeyDeleteAsync(sessionKey, ct);

        if (string.IsNullOrEmpty(json))
            return;

        var entry = Deserialize(json);
        if (entry is null)
            return;

        await redis.HashDeleteAsync($"{UserSessionsKeyPrefix}{entry.UserGuid}", fingerprint, ct);
        await jwtTokenService.RevokeTokenAsync(entry.AccessToken, entry.ExpiresAt);
        if (!string.IsNullOrEmpty(entry.RefreshToken))
            await jwtTokenService.RevokeTokenAsync(entry.RefreshToken);

        logger.LogInformation("[TokenSession] 会话已登出: UserId={UserId}", entry.UserGuid);
    }

    public async Task RevokeAllSessionsAsync(Guid userGuid, CancellationToken ct = default)
    {
        var userKey = $"{UserSessionsKeyPrefix}{userGuid}";
        var fingerprints = (await redis.HashKeysAsync(userKey, ct)).ToList();

        foreach (var fingerprint in fingerprints)
        {
            var sessionKey = $"{SessionKeyPrefix}{fingerprint}";
            var json = await redis.StringGetAsync(sessionKey, ct);
            await redis.KeyDeleteAsync(sessionKey, ct);

            var entry = Deserialize(json);
            if (entry is null)
                continue;

            await jwtTokenService.RevokeTokenAsync(entry.AccessToken, entry.ExpiresAt);
            if (!string.IsNullOrEmpty(entry.RefreshToken))
                await jwtTokenService.RevokeTokenAsync(entry.RefreshToken);
        }

        await redis.KeyDeleteAsync(userKey, ct);
        logger.LogInformation("[TokenSession] 用户全部会话已吊销: UserId={UserId}, Count={Count}",
            userGuid, fingerprints.Count);
    }

    private TokenSessionEntry? Deserialize(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return null;
        try
        {
            return JsonSerializer.Deserialize<TokenSessionEntry>(json);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "[TokenSession] 会话条目反序列化失败，已忽略");
            return null;
        }
    }

    private static string Fingerprint(string token)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)))[..32].ToLowerInvariant();
}

/// <summary>
/// 单设备会话条目（Redis JSON）
/// </summary>
public sealed record TokenSessionEntry(
    Guid UserGuid,
    string AccessToken,
    string? RefreshToken,
    DateTimeOffset ExpiresAt);
