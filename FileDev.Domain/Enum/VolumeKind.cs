namespace FileDev.Domain.Enum;

/// <summary>
/// 数据卷类型：与 Mono.FileBox.Lite 存储的卷归属语义对齐（默认/共享/租户专属）。
/// 卷 ID 为空串表示默认卷；卷归属租户为空且非默认卷表示共享卷池卷；否则为某租户的专属卷。
/// </summary>
public enum VolumeKind
{
    /// <summary>默认卷：承载清单与系统区，卷 ID 为空串。</summary>
    Default = 0,

    /// <summary>共享数据卷：所有租户共用（卷归属租户为空）。</summary>
    Shared = 1,

    /// <summary>租户专属数据卷：仅该租户写入（卷归属租户非空）。</summary>
    Tenant = 2
}