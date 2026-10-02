using Mono.FileBox.Lite.Abstractions.Configuration;
using Mono.FileBox.Lite.Abstractions.Storage;
using Mono.FileBox.Lite.Storage;

namespace FileDev.Infrastructure.Service.FileBox;

/// <summary>
/// 按文件类别将写入路由到固定磁盘池。FileBox 默认 <c>ConsistentHashDiskSelector</c> 忽略 <see cref="WriteOptions.PoolId"/>
/// （按内容哈希环路由），而本文档要求「个人文件 → user-repo 池 / 内容附件 → content-attachment 池」的物理隔离，
/// 因此自定义该选择器：写入时按 <see cref="WriteOptions.PoolId"/> 强制命中对应池；未指定时回退第一个启用池。
/// <para>
/// 读取仍按内容寻址（同一内容任意池字节一致），读路径回退到首个启用池即可保证正确性，物理层策略不参与读判。
/// </para>
/// </summary>
public sealed class CategoryPoolSelector : IDiskSelector
{

    private readonly IReadOnlyList<PoolOptions> _pools;

    
    private readonly IReadOnlyDictionary<string, PoolOptions> _byId;

    public CategoryPoolSelector(FileBoxOptions options)
    {
        var pools = options?.Storage?.Pools ?? [];
        _pools = pools.Where(p => p.Enabled && !string.IsNullOrEmpty(p.PoolId)).ToList();
        _byId = _pools.ToDictionary(p => p.PoolId!, p => p);
    }

    /// <inheritdoc/>
    public Task<IDiskHandle> SelectForWriteAsync(string contentHash, WriteOptions options, CancellationToken ct)
    {
        var pool = ResolvePool(options?.PoolId);
        return Task.FromResult<IDiskHandle>(HandleOf(pool));
    }

    /// <inheritdoc/>
    public Task<IDiskHandle> SelectForReadAsync(string contentHash, CancellationToken ct)
    {
        // 内容寻址：任意池读到的字节一致，读回退到首个启用池。
        var pool = ResolvePool(null);
        return Task.FromResult<IDiskHandle>(HandleOf(pool));
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<DiskPoolInfo>> ListPoolsAsync(CancellationToken ct)
        => Task.FromResult<IReadOnlyList<DiskPoolInfo>>(_pools.Select(ToPoolInfo).ToList());

    /// <summary>按池 ID 解析池；未指定或不存在时回退首个启用池。</summary>
    private PoolOptions ResolvePool(string? poolId)
    {
        if (!string.IsNullOrEmpty(poolId) && _byId.TryGetValue(poolId, out var hit))
            return hit;
        return _pools.FirstOrDefault()
               ?? throw new InvalidOperationException("No storage pool is configured.");
    }

    private static IDiskHandle HandleOf(PoolOptions pool)
        => new LocalDiskHandle(pool.PoolId!, pool.RootPath ?? pool.PoolId!);

    private static DiskPoolInfo ToPoolInfo(PoolOptions pool) => new()
    {
        PoolId = pool.PoolId,
        RootPath = pool.RootPath,
        Tier = pool.Tier,
        CapacityBytes = pool.CapacityBytes,
        Priority = pool.Priority,
        Enabled = pool.Enabled,
        HealthyDiskCount = 1
    };
}