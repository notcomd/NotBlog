namespace FileDev.Domain.Entities;

/// <summary>
/// 分片上传任务状态枚举。
/// </summary>
public enum ChunkUploadStatus
{
    /// <summary>待上传</summary>
    Pending,

    /// <summary>上传中</summary>
    Uploading,

    /// <summary>已合并完成</summary>
    Merged,

    /// <summary>上传失败</summary>
    Failed,

    /// <summary>已取消</summary>
    Cancelled
}
