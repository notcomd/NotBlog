namespace Notcomd.DomainCommand;

public static class StringExtension
{

    /// <summary>
    ///     扩展方法字符串忽略大小写比较
    /// </summary>
    /// <param name="str">扩展方法</param>
    /// <param name="str2">需要判断的字符串</param>
    /// <returns>
    /// true:相等 false:不相等
    /// </returns>
    public static bool EqualsIgnoreCase(this string str, string str2)
    {
        return string.Equals(str, str2, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///  字符串大小写比较
    /// </summary>
    /// <param name="str">
    /// 扩展字符串
    /// </param>
    /// <param name="str2">需要进行判断的字符串</param>
    /// <returns>
    /// true:相等 false:不相等
    /// </returns>
    public static bool EqualsCase(this string str, string str2)
    {
        return string.Equals(str, str2, StringComparison.Ordinal);
    }

    /// <summary>
    ///     截取片段
    /// </summary>
    /// <param name="str"></param>
    /// <param name="length">截取长度</param>
    /// <returns>返回截取的字符串</returns>
    public static string Cut(this string str, int length)
    {
        var lent = str.Length <= length ? str.Length : length;
        return str[..lent];
    }
}