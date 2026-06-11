namespace Identity.Domain.Entities.UserAggregate;

public class UserLoginHistory : Entity
{
    private UserLoginHistory()
    {
        LoginGuid = Guid.CreateVersion7();
        CreateDataTime = DateTime.Now;
    }

    public UserLoginHistory(Guid userId, PhoneNumber phoneNumber,
        string loginMessage, string email) : this()
    {
        {
            UserGuid = userId;
            Email = email;
            PhoneNumber = phoneNumber;
            LoginMessage = loginMessage;
        }
    }

    /// <summary>
    /// 登录历史ID
    /// </summary>
    public Guid LoginGuid { get; init; }

    /// <summary>
    /// 用户ID
    /// </summary>
    public Guid UserGuid { get; init; }

    /// <summary>
    /// 手机号
    /// </summary>
    public PhoneNumber PhoneNumber { get; init; }

    /// <summary>
    /// 邮箱
    /// </summary>
    [EmailAddress(ErrorMessage = "Error Email Address!")]

    public string? Email { get; set; }

    /// <summary>
    /// 登录时间
    /// </summary>
    public DateTime CreateDataTime { get; init; }

    /// <summary>
    /// 登录消息
    /// </summary>
    public string? LoginMessage { get; set; }
}