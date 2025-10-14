
namespace Markdown.Domain.Entities;

public enum BlockType
{
    /// <summary>
    /// 文本块
    /// </summary>
    Text,
    /// <summary>
    /// 图片块
    /// </summary>
    Image,
    /// <summary>
    /// 代码块
    /// </summary>
    Code,
    /// <summary>
    /// 引用块
    /// </summary>
    Quote,
    /// <summary>
    /// 列表块
    /// </summary>
    List,
    /// <summary>
    /// 表格块
    /// </summary>
    Table,
    /// <summary>
    /// 链接块
    /// </summary>
    Link,
    /// <summary>
    /// 标题块
    /// </summary>
    Heading,
    /// <summary>
    /// 水平规则块
    /// </summary>
    HorizontalRule,
    /// <summary>
    /// 未知块类型
    /// </summary>
    Unknow
}
