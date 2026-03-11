namespace Identity.Domain.AggregatesModel.UserAggregate;

public enum UserPrivacy
{
    Public = 0, //公开

    Private = 1, //私密

    Friends = 2, //好友可见

    Custom = 3 //自定义
}