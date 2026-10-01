namespace FileDev.Domain.Enum;

/// <summary>
/// 存储层，与 Mono.FileBox.Lite 存储的 StorageTier 对齐（Hot/Cold 领域语义）。
/// 用于描述文件在 FileBox 对象存储中的冷热分层，供生命周期扫描将久未访问的 Hot 对象降为 Cold。
/// </summary>
public enum StorageTier
{
    /// <summary>热存储：默认写入层，访问频次高。</summary>
    Hot = 0,

    /// <summary>冷存储：生命周期分层降级后的层，成本更低。</summary>
    Cold = 1
}