namespace Message.Domain.Enums;

public enum RecallReason
{
    /// <summary>
    /// 用户请求撤回
    /// </summary>
    UserRequest,

    /// <summary>
    /// 系统错误撤回
    /// </summary>
    SystemError,

    /// <summary>
    /// 政策违规撤回
    /// </summary>
    PolicyViolation
}