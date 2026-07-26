namespace Video.Domain.ValueObjects;

/// <summary>
/// 评论内容类型常量
/// </summary>
public static class ReviewContentType
{
    public const string Text = "text";
    public const string Image = "image";
    public const string Video = "video";
    public const string RichText = "richtext";

    private static readonly HashSet<string> ValidTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        Text, Image, Video, RichText
    };

    public static bool IsValid(string type) => ValidTypes.Contains(type);

    public static string Normalize(string type)
    {
        return type?.ToLowerInvariant() switch
        {
            Text => Text,
            Image => Image,
            Video => Video,
            RichText => RichText,
            _ => throw new ArgumentException($"Unknown content type: '{type}'. " +
                $"Valid types: {Text}, {Image}, {Video}, {RichText}.", nameof(type))
        };
    }
}
