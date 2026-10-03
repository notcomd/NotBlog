namespace Video.Domain.Enums;

/// <summary>
/// 弹幕内容类型枚举。
/// </summary>
public enum BarrageType
{
    /// <summary>纯文本弹幕（向后兼容默认值）</summary>
    Text = 0,

    /// <summary>纯图片弹幕</summary>
    Image = 1,

    /// <summary>文本+图片混合弹幕</summary>
    Mixed = 2
}
