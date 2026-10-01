using FileDev.Domain.Dto.Response;
using FileDev.Domain.Enum;
using FileDev.Domain.IServices;
using Microsoft.Extensions.Logging;
using Mono.FileBox.Lite.Abstractions.Index;
using Notcomd.Token.JWT.Core;
using Notcomd.Token.JWT.Security;
using DomainStorageTier = FileDev.Domain.Enum.StorageTier;
using AbstractionsIndexStorageTier = Mono.FileBox.Lite.Abstractions.Index.StorageTier;

namespace FileDev.Infrastructure.Service;

// 对象清单与存储层管理：把 FileBox 索引条目映射为响应元数据、读取对象清单、调整（软）存储层。
public sealed partial class FileBoxObjectStorageService
{
    /// <summary>FileDev 领域分层 Hot/Cold 映射为 FileBox 索引存储层（Hot/Warm/Cold/Archive）。</summary>
    private static AbstractionsIndexStorageTier MapTierToFileBox(DomainStorageTier tier)
        => tier switch
        {
            DomainStorageTier.Cold => AbstractionsIndexStorageTier.Cold,
            _ => AbstractionsIndexStorageTier.Hot
        };

    /// <summary>FileBox 索引存储层映射回 FileDev 领域分层（Warm/Archive 均视为 Cold）。</summary>
    private static DomainStorageTier MapTierFromFileBox(AbstractionsIndexStorageTier tier)
        => tier switch
        {
            AbstractionsIndexStorageTier.Hot => DomainStorageTier.Hot,
            _ => DomainStorageTier.Cold
        };

    /// <summary>将 FileBox 索引条目映射为响应中的存储元数据（卷 ID / 分片数 / 分层 / 更新时间）。</summary>
    private static NotFileStorageResponse ApplyManifestMeta(NotFileStorageResponse response, IndexEntry entry)
    {
        response.ContentHash = entry.ContentHash ?? response.ContentHash;
        response.Tier = MapTierFromFileBox(entry.Tier);
        response.VolumeId = "default";
        response.ShardCount = 1;
        response.UpdatedUtc = entry.ModifiedAt;
        return response;
    }

    /// <summary>读取对象清单；对象不存在或读取异常时返回 null。</summary>
    /// <param name="fileRelativePath">对象 key。</param>
    /// <param name="ct">取消令牌。</param>
    /// <param name="ctx">存储上下文（租户决定命名空间）。</param>
    public async Task<StorageManifestDto?> GetManifestAsync(string fileRelativePath, CancellationToken ct = default,
        StoreContext? ctx = null)
    {
        try
        {
            var entry = await ResolveEntryAsync(fileRelativePath, NsOf(EffectiveTenant(ctx)), ct).ConfigureAwait(false);
            if (entry is null)
                return null;
            return new StorageManifestDto
            {
                VolumeId = "default",
                ShardCount = 1,
                ContentHash = entry.ContentHash ?? string.Empty,
                Tier = MapTierFromFileBox(entry.Tier),
                UpdatedUtc = entry.ModifiedAt,
                ExpiresAt = null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "读取对象清单失败 Path={Path}", fileRelativePath);
            return null;
        }
    }

    /// <summary>按 ObjectKey 精确反查索引条目；对象不存在返回 null。</summary>
    private async Task<IndexEntry?> ResolveEntryAsync(string fileRelativePath, string namespaceId,
        CancellationToken ct = default)
    {
        var page = await _index.QueryAsync(new IndexQuery
        {
            NamespaceId = namespaceId,
            KeyPrefix = fileRelativePath
        }, ct).ConfigureAwait(false);
        return page.Items.FirstOrDefault(e => e.ObjectKey == fileRelativePath);
    }

    /// <summary>调整对象存储层（软分层：FileBox 无逐对象迁移 API，此处记录目标分层并映射返回）。</summary>
    /// <param name="fileRelativePath">对象 key。</param>
    /// <param name="tier">目标存储层（领域 Hot/Cold）。</param>
    /// <param name="ct">取消令牌。</param>
    /// <param name="ctx">存储上下文（租户决定命名空间）。</param>
    public async Task<StorageManifestDto?> ChangeStorageTierAsync(string fileRelativePath, DomainStorageTier tier,
        CancellationToken ct = default, StoreContext? ctx = null)
    {
        try
        {
            var entry = await ResolveEntryAsync(fileRelativePath, NsOf(EffectiveTenant(ctx)), ct).ConfigureAwait(false);
            if (entry is null)
            {
                _logger.LogWarning("调整存储层失败（对象不存在） Path={Path}", fileRelativePath);
                return null;
            }

            // FileBox 无逐对象迁移 API；此处仅返回带目标分层的清单（软语义），
            // 实际分层变更由 FileBox 集群/生命周期层（TierManager/Scaling）驱动，本适配层不触发物理迁移。
            return new StorageManifestDto
            {
                VolumeId = "default",
                ShardCount = 1,
                ContentHash = entry.ContentHash ?? string.Empty,
                Tier = tier,
                UpdatedUtc = entry.ModifiedAt,
                ExpiresAt = null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "调整存储层失败 Path={Path}", fileRelativePath);
            return null;
        }
    }
}