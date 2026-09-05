namespace Message.Domain.ValueObjects.Message;

/// <summary>
/// 消息内容抽象基类（多态）。
/// <para>
/// 将 Message 实体上按类型拍平悬浮的内容字段收敛为单一内容值对象；
/// 各消息类型（文本/媒体/文件/位置/链接/表情）由具体子类承载，
/// Message 仅持有 <c>Content</c> 引用，实现聚合内聚与开闭原则。
/// 通过 <see cref="MessageType"/> 判别具体类型，持久化层据此完成读改写映射。
/// </para>
/// </summary>
public abstract class MessageContent : ValueObject
{
    /// <summary>
    /// 内容的业务类型（与消息类型一一对应，作为持久化判别字段）。
    /// </summary>
    public abstract MessageType MessageType { get; }

    /// <summary>
    /// 生成会话侧栏的「最后消息」摘要文本（如 [图片] / [位置] xxx / 正文）。
    /// </summary>
    public abstract string ToSessionSummary();
}