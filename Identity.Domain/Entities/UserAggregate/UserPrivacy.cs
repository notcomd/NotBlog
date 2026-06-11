namespace Identity.Domain.Entities.UserAggregate;

/// <summary>
/// 用户隐私设置
/// </summary>
public enum UserPrivacy
{
    Public = 0, //公开

    Private = 1, //私密

    Friends = 2, //好友可见

    Custom = 3 //自定义
}