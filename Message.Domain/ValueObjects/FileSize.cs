using Commons.SeedWork;

namespace Message.Domain.ValueObjects;

public class FileSize : ValueObject
{
    public FileSize(long bytes)
    {
        if (bytes < 0)
            throw new ArgumentException("文件大小不能为负数", nameof(bytes));
        Bytes = bytes;
    }

    public long Bytes { get; }

    public double Kilobytes => Bytes / 1024.0;
    public double Megabytes => Bytes / (1024.0 * 1024.0);
    public double Gigabytes => Bytes / (1024.0 * 1024.0 * 1024.0);

    public static FileSize FromBytes(long bytes) => new(bytes);
    public static FileSize FromKilobytes(double kilobytes) => new((long)(kilobytes * 1024));
    public static FileSize FromMegabytes(double megabytes) => new((long)(megabytes * 1024 * 1024));
    public static FileSize FromGigabytes(double gigabytes) => new((long)(gigabytes * 1024 * 1024 * 1024));

    public string GetFormattedSize()
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        double size = Bytes;

        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size = size / 1024;
        }

        return $"{size:0.##} {sizes[order]}";
    }

    public bool IsLargerThan(FileSize other) => Bytes > other.Bytes;
    public bool IsSmallerThan(FileSize other) => Bytes < other.Bytes;

    public static FileSize operator +(FileSize left, FileSize right) => new(left.Bytes + right.Bytes);
    public static FileSize operator -(FileSize left, FileSize right) => new(left.Bytes - right.Bytes);
    public static bool operator >(FileSize left, FileSize right) => left.Bytes > right.Bytes;
    public static bool operator <(FileSize left, FileSize right) => left.Bytes < right.Bytes;
    public static bool operator >=(FileSize left, FileSize right) => left.Bytes >= right.Bytes;
    public static bool operator <=(FileSize left, FileSize right) => left.Bytes <= right.Bytes;

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Bytes;
    }

    public override string ToString() => GetFormattedSize();
}