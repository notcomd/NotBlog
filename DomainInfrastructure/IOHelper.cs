namespace Notcomd.DomainCommand;

public static class IOHelper
{
    /// <summary>
    ///     将流转换为字节数组
    /// </summary>
    /// <param name="stream"></param>
    /// <returns></returns>
    public static async Task<byte[]> ToArrayAsync(this Stream stream)
    {
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory);
        memory.Position = 0;
        return memory.ToArray();
    }

    /// <summary>
    ///     将流转换为字节数组
    /// </summary>
    /// <param name="stream"></param>
    /// <returns></returns>
    public static byte[] ToArrsy(this Stream stream)
    {
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        memory.Position = 0;
        return memory.ToArray();
    }

    /// <summary>
    ///     创建文件夹
    /// </summary>
    /// <param name="fileInfo"></param>
    public static void CreateDir(FileInfo fileInfo)
    {
        if (!fileInfo.Directory.Exists) fileInfo.Directory.Create();
    }
}