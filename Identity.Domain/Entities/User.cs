using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Identity.Domain.Entities;

public class User : IAggregateRoot
{
    private User()
    {
    }

    public User(string userEmail, PhoneNumber phoneNumber, string userName, Guid userRoleGuid, string passwordHash)
    {
        UserGuid = Guid.NewGuid();
        UserName = userName;
        UserEmail = userEmail;
        UserPhone = phoneNumber;
        UserRoleGuid = userRoleGuid;
        PasswordHash = passwordHash;
        CreateDatetime = DateTime.Now.ToUniversalTime();
        UserAccessFail = new UserAccessFail(this);
    }

    public User(Guid userRoleGuid, string userEmail, string passwordHash, string salt)
    {
        UserGuid = Guid.NewGuid();
        UserEmail = userEmail;
        UserRoleGuid = userRoleGuid;
        PasswordHash = passwordHash;
        Salt = salt;
        CreateDatetime = DateTime.Now;
        UserAccessFail = new UserAccessFail(this);
    }

    public User(Guid userRoleGuid, PhoneNumber phoneNumber, string passwordHash, string salt)
    {
        UserGuid = Guid.NewGuid();
        UserRoleGuid = userRoleGuid;
        this.UserPhone = phoneNumber;
        this.PasswordHash = passwordHash;
        Salt = salt;
        CreateDatetime = DateTime.UtcNow.ToUniversalTime();
        UserAccessFail = new UserAccessFail(this);
    }


    public Guid UserGuid { get; init; }
    public Guid UserRoleGuid { get; init; }
    public string? UserName { get; private set; }
    public string? UserEmail { get; private set; }
    public string PasswordHash { get; private set; }
    public string Salt { get; private set; }
    public PhoneNumber? UserPhone { get; private set; }
    public string? UserAddress { get; private set; }
    [Column(TypeName = "timestamp with time zone")]
    public DateTimeOffset CreateDatetime { get; init; }
    public UserAccessFail UserAccessFail { get; init; }
    public BlackOrWhite? BlackOrWhite { get; private set; }


    public ValueTask<User> ChangeByAddressAsync(ref string userAddress)
    {
        UserAddress = userAddress;
        return new ValueTask<User>(this);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="password"></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public async ValueTask ChangeByPasswordAsync(string password)
    {
        if (password.Length <= 8)
        {
            throw new ArgumentOutOfRangeException("your are set password is short!");
        }
        PasswordHash = await HashH256Tool.CreateHash256Async(password, Encoding.UTF8.GetBytes(this.Salt));
    }


    /// <summary>
    ///     设置信邮箱
    /// </summary>
    /// <param name="newEmail"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public ValueTask ChangeByEmailAsync(
        [EmailAddress(ErrorMessage = "your set email is error ,pleas set again your email address!")]
        ref string newEmail)
    {
        if (UserEmail == newEmail) throw new ArgumentException("需要不同的邮箱");
        UserEmail = newEmail;
        return ValueTask.CompletedTask;
    }

    /// <summary>
    ///    设置新密码
    /// </summary>
    /// <param name="phoneNumber"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public ValueTask ChangeByPhoneAsync(PhoneNumber phoneNumber)
    {
        if (phoneNumber.PhoneCode == UserPhone!.PhoneCode) throw new ArgumentException("需要不要一样的号码");
        UserPhone = phoneNumber;
        return ValueTask.CompletedTask;
    }

    public ValueTask ChangeByEmailAsync(string emailAddress)
    {
        if (this.UserEmail == emailAddress)
        {
            return ValueTask.CompletedTask;
        }
        UserEmail = emailAddress;
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// 验证密码是否正确
    /// </summary>
    /// <param name="hashPassword">hash密码</param>
    /// <param name="password">密码</param>
    /// <param name="salt">加盐</param>
    /// <returns></returns>
    public ValueTask<bool> CheckByPasswordAsync(string hashPassword, string password, byte[] salt) => HashH256Tool.VerifyPasswordValueTask(password: password, hash: hashPassword, sart: salt);


    public ValueTask AddBlackOrWhiteValueTask(BlackOrWhite blackOrWhite)
    {
        BlackOrWhite = blackOrWhite;
        return ValueTask.CompletedTask;
    }


    public async ValueTask<User> ChangeByPasswordValueTask(string password, byte[] salt)
    {
        if (!HashH256Tool.VerifyPasswordValueTask(password, this.PasswordHash, salt).GetAwaiter().GetResult())
        {
            this.PasswordHash = await HashH256Tool.CreateHash256Async(password, salt);
        }
        return this;
    }
}