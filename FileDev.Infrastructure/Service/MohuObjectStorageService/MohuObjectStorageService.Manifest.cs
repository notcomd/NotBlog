using FileDev.Domain.Dto.Response;
using Microsoft.Extensions.Logging;
using MohuTianchi.Lite;
using Notcomd.Token.JWT.Core;
using Notcomd.Token.JWT.Security;
using StorageTier = FileDev.Domain.Enum.StorageTier;
using LiteStorageTier = MohuTianchi.Lite.StorageTier;

namespace FileDev.Infrastructure.Service;

// 对象清单与存储层管理：把 Lite 清单元数据回填到响应、读取对象清单、切换 Hot/Cold 存储层。
public sealed partial class MohuObjectStorageService
{
    /// <summary>将 Lite 清单回填到响应中的存储元数据（卷 ID / 分片数 / 更新时间 / 过期时间）。</summary>
    /// <param name="response">待回填的响应。</param>
    /// <param name="key">对象 key。</param>
    private async Task ApplyManifestMetaAsync(NotFileStorageResponse response, string key)
    {
        try
        {
            var manifest = await _storage.GetManifestAsync(key, ct: CancellationToken.None).ConfigureAwait(false);
            if (manifest is null) return;
            response.VolumeId = manifest.VolumeId ?? string.Empty;
            response.ShardCount = manifest.Shards?.Count ?? 1;
            response.ExpiresAt = manifest.ExpiresAt;
            response.UpdatedUtc = manifest.UpdatedUtc;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "回填清单元数据失败 Key={Key}", key);
        }
    }

    /// <summary>读取对象清单；对象不存在或读取异常时返回 null。</summary>
    /// <param name="fileRelativePath">对象 key。</param>
    /// <param name="ct">取消令牌。</param>
    public async Task<StorageManifestDto?> GetManifestAsync(string fileRelativePath, CancellationToken ct = default)
    {
        try
        {
            var manifest = await _storage.GetManifestAsync(fileRelativePath, ct: ct).ConfigureAwait(false);
            if (manifest is null) return null;
            return new StorageManifestDto
            {
                VolumeId = manifest.VolumeId ?? string.Empty,
                ShardCount = manifest.Shards?.Count ?? 1,
                ContentHash = HashHelper.ComputeHash(
                    (await _storage.ReadAsync(fileRelativePath, ct: ct).ConfigureAwait(false)) ?? [], AlgorithmType.SHA256),
                Tier = StorageTier.Hot,
                ExpiresAt = manifest.ExpiresAt,
                UpdatedUtc = manifest.UpdatedUtc
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "读取对象清单失败 Path={Path}", fileRelativePath);
            return null;
        }
    }

    /// <summary>调整对象存储层（Hot→Cold），返回改动后的清单；对象不存在或调整失败返回 null。</summary>
    /// <param name="fileRelativePath">对象 key。</param>
    /// <param name="tier">目标存储层。</param>
    /// <param name="ct">取消令牌。</param>
    public async Task<StorageManifestDto?> ChangeStorageTierAsync(string fileRelativePath, StorageTier tier,
        CancellationToken ct = default)
    {
        try
        {
            if (!await _storage.ExistsAsync(fileRelativePath, ct: ct).ConfigureAwait(false))
            {
                _logger.LogWarning("调整存储层失败（对象不存在） Path={Path}", fileRelativePath);
                return null;
            }

            await _storage.UpdateTierAsync(fileRelativePath, (LiteStorageTier)tier, ct: ct).ConfigureAwait(false);
            var manifest = await GetManifestAsync(fileRelativePath, ct).ConfigureAwait(false);
            if (manifest is not null)
                manifest.Tier = tier;
            return manifest;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "调整存储层失败 Path={Path}", fileRelativePath);
            return null;
        }
    }
}