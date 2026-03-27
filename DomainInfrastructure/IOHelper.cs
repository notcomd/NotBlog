namespace DomainInfrastructure;

/// <summary>
/// IO 操作扩展方法
/// </summary>
public static class IOHelper
{
    /// <summary>异步将流转换为字节数组</summary>
    public static async Task<byte[]> ToArrayAsync(this Stream stream)
    {
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory);
        memory.Position = 0;
        return memory.ToArray();
    }

    /// <summary>同步将流转换为字节数组</summary>
    public static byte[] ToByteArray(this Stream stream)
    {
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        memory.Position = 0;
        return memory.ToArray();
    }

    /// <summary>确保文件所在目录存在，不存在则创建</summary>
    public static void EnsureDirectoryExists(this FileInfo fileInfo)
    {
        if (fileInfo.Directory is { Exists: false })
            fileInfo.Directory.Create();
    }
}