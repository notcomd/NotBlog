namespace Message.Domain.IServices;

/// <summary>
/// 消息撤回策略（检查撤回是否在时限内）。
/// <para>
/// 拆分设计：将"撤回时限规则"从 Chat 上下文的 Message 聚合中解耦，
/// 避免 Message 聚合直接依赖 Recall 上下文的 MessageRecall 实体，
/// 实现有界上下文隔离。
/// </para>
/// </summary>
public interface IMessageRecallPolicy
{
    /// <summary>
    /// 检查给定消息发送时间是否仍在可撤回时限内。
    /// </summary>
    /// <param name="sentTime">消息发送时间（UTC）</param>
    /// <returns>是否可撤回</returns>
    bool CanRecall(DateTime sentTime);
}
