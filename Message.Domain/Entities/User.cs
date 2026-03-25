using Message.Domain.Enums;
using Message.Domain.SeedWork;

namespace Message.Domain.Entities;

public class User : Entity, IAggregateRoot
{
    public User(Guid userId, string userName)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("用户ID不能为空", nameof(userId));
        if (string.IsNullOrWhiteSpace(userName))
            throw new ArgumentException("用户名不能为空", nameof(userName));

        UserId = userId;
        UserName = userName;
        Status = UserStatus.Offline;
        LastOnlineTime = DateTime.UtcNow;
        CreatedTime = DateTime.UtcNow;
        IsDeleted = false;
    }

    private User()
    {
        UserId = Guid.NewGuid();
        CreatedTime = DateTime.UtcNow;
    }

    public Guid UserId { get; init; }
    public string UserName { get; set; } = null!;
    public string? NickName { get; set; }
    public Uri? Avatar { get; set; }

    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }
    public UserStatus Status { get; private set; }
    public DateTime LastOnlineTime { get; private set; }
    public DateTime CreatedTime { get; init; }
    public DateTime? UpdatedTime { get; private set; }
    public bool IsDeleted { get; private set; }

    public void UpdateProfile(string? userName, string? nickName, Uri? avatar)
    {
        if (!string.IsNullOrWhiteSpace(userName))
            UserName = userName;
        NickName = nickName;
        Avatar = avatar;
        UpdatedTime = DateTime.UtcNow;
    }

    public void SetOnline()
    {
        Status = UserStatus.Online;
        LastOnlineTime = DateTime.UtcNow;
        UpdatedTime = DateTime.UtcNow;
    }

    public void SetOffline()
    {
        Status = UserStatus.Offline;
        LastOnlineTime = DateTime.UtcNow;
        UpdatedTime = DateTime.UtcNow;
    }

    public void SetStatus(UserStatus status)
    {
        Status = status;
        LastOnlineTime = DateTime.UtcNow;
        UpdatedTime = DateTime.UtcNow;
    }

    public void Delete()
    {
        if (IsDeleted)
            throw new InvalidOperationException("用户已被删除");
        IsDeleted = true;
        UpdatedTime = DateTime.UtcNow;
    }

    public void Restore()
    {
        if (!IsDeleted)
            throw new InvalidOperationException("用户未被删除");
        IsDeleted = false;
        UpdatedTime = DateTime.UtcNow;
    }
}