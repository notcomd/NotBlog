namespace Message.Domain.Enums;

public enum GroupPermission
{
    /// <summary>
    /// 发送消息权限
    /// </summary>
    SendMessage,

    /// <summary>
    /// 邀请成员权限
    /// </summary>
    InviteMember,

    /// <summary>
    /// 编辑群信息权限
    /// </summary>
    EditGroupInfo,

    /// <summary>
    /// 移除成员权限
    /// </summary>
    RemoveMember,

    /// <summary>
    /// 禁言成员权限
    /// </summary>
    MuteMember,

    /// <summary>
    /// 解禁言成员权限
    /// </summary>
    BanMember,

    /// <summary>
    /// 转让群主权限
    /// </summary>
    /// </summary>
    TransferOwnership
}