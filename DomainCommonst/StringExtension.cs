namespace DomainCommonst;

public static class StringExtension
{

    /// <summary>
    /// 字符串忽略大小写比较
    /// </summary>
    /// <param name="str"></param>
    /// <param name="str2"></param>
    /// <returns></returns>
    public static bool EqualsIgnoreCase(this string str, string str2)
    {
        return string.Equals(str, str2, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 截取片段
    /// </summary>
    /// <param name="str"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    public static string Cut(this String str, int length)
    {
        if (str is null) return string.Empty;
        var lent = str.Length <= length ? str.Length : length;
        return str[0..lent];
    }
}