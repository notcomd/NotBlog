using Message.Domain.SeedWork;

namespace Message.Domain.ValueObjects.Message;

/// <summary>
/// 文件消息内容值对象。
/// 不可变，封装文件 URI 与名称/大小/MIME 约束。
/// </summary>
public class FileContent : ValueObject
{
    private FileContent(Uri fileUri, string fileName, long fileSize, string mimeType)
    {
        FileUri = fileUri ?? throw new ArgumentNullException(nameof(fileUri));

        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("文件名不能为空");

        if (fileSize <= 0)
            throw new ArgumentException("文件大小必须大于0");

        if (string.IsNullOrWhiteSpace(mimeType))
            throw new ArgumentException("MIME类型不能为空");

        FileName = fileName;
        FileSize = fileSize;
        MimeType = mimeType;
    }

    public Uri FileUri { get; }
    public string FileName { get; }
    public long FileSize { get; }
    public string MimeType { get; }

    public static FileContent Create(Uri fileUri, string fileName, long fileSize, string mimeType)
        => new(fileUri, fileName, fileSize, mimeType);

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return FileUri;
        yield return FileName;
        yield return FileSize;
        yield return MimeType;
    }
}
