namespace FileDev.Domain.ValueObjects;

public class FileSize : IEquatable<FileSize>
{
    public FileSize(double bytes)
    {
        if (bytes < 0)
            throw new ArgumentOutOfRangeException(nameof(bytes), "文件大小不能为负数");

        Bytes = bytes;
    }

    public double Bytes { get; }

    public double Kilobytes => Bytes / 1024;
    public double Megabytes => Bytes / (1024 * 1024);
    public double Gigabytes => Bytes / (1024 * 1024 * 1024);

    public bool Equals(FileSize? other)
    {
        if (other is null)
            return false;

        return Math.Abs(Bytes - other.Bytes) < 0.001;
    }

    public static FileSize FromKilobytes(double kilobytes)
    {
        return new FileSize(kilobytes * 1024);
    }

    public static FileSize FromMegabytes(double megabytes)
    {
        return new FileSize(megabytes * 1024 * 1024);
    }

    public static FileSize FromGigabytes(double gigabytes)
    {
        return new FileSize(gigabytes * 1024 * 1024 * 1024);
    }

    public string ToReadableString()
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        var order = 0;
        var size = Bytes;

        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size = size / 1024;
        }

        return $"{size:0.##} {sizes[order]}";
    }

    public override bool Equals(object? obj)
    {
        if (obj is null)
            return false;

        if (obj.GetType() != GetType())
            return false;

        return Equals((FileSize)obj);
    }

    public override int GetHashCode()
    {
        return Bytes.GetHashCode();
    }

    public static bool operator ==(FileSize? left, FileSize? right)
    {
        if (ReferenceEquals(left, right))
            return true;

        if (left is null || right is null)
            return false;

        return left.Equals(right);
    }

    public static bool operator !=(FileSize? left, FileSize? right)
    {
        return !(left == right);
    }

    public static bool operator <(FileSize? left, FileSize? right)
    {
        if (left is null || right is null)
            throw new ArgumentNullException();

        return left.Bytes < right.Bytes;
    }

    public static bool operator >(FileSize? left, FileSize? right)
    {
        if (left is null || right is null)
            throw new ArgumentNullException();

        return left.Bytes > right.Bytes;
    }

    public static FileSize operator +(FileSize left, FileSize right)
    {
        if (left is null || right is null)
            throw new ArgumentNullException();

        return new FileSize(left.Bytes + right.Bytes);
    }

    public static FileSize operator -(FileSize left, FileSize right)
    {
        if (left is null || right is null)
            throw new ArgumentNullException();

        return new FileSize(left.Bytes - right.Bytes);
    }

    public override string ToString()
    {
        return ToReadableString();
    }
}