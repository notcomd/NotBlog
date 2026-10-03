
namespace Message.Domain.ValueObjects.Message;

/// <summary>
/// 文件消息内容值对象。
/// 不可变，封装文件 URI 与名称/大小/MIME 约束。
/// </summary>
public class FileContent : MessageContent
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

    /// <summary>文件 URI（非空）</summary>
    public Uri FileUri { get; }
    /// <summary>文件名（非空）</summary>
    public string FileName { get; }
    /// <summary>文件大小（字节，大于 0）</summary>
    public long FileSize { get; }
    /// <summary>MIME 类型（非空）</summary>
    public string MimeType { get; }

    /// <summary>内容业务类型，恒为文件消息。</summary>
    public override MessageType MessageType => MessageType.MessageFile;

    /// <summary>创建文件内容值对象（校验文件名、大小与 MIME 类型）。</summary>
    public static FileContent Create(Uri fileUri, string fileName, long fileSize, string mimeType)
        => new(fileUri, fileName, fileSize, mimeType);

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return FileUri;
        yield return FileName;
        yield return FileSize;
        yield return MimeType;
    }

    /// <summary>生成会话侧栏摘要，形如「[文件] 文件名」。</summary>
    public override string ToSessionSummary() => $"[文件] {FileName}";
}
