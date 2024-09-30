using System.ComponentModel.DataAnnotations;

namespace Identity.Domain.Entities;

public class User : IAggregateRoot
{
    private string PasswordHash = null!;


    private User()
    {
    }

    public User(ref string userEmail, PhoneNumber phoneNumber, string userName, Guid userRoleGuid)
    {
        UserGuid = new Guid();
        UserName = userName;
        UserEmail = userEmail;
        UserPhone = phoneNumber;
        UserRoleGuid = userRoleGuid;
        CreateDatetime = DateTime.Now;
        UserAccessFail = new UserAccessFail(this);
    }

    public Guid UserGuid { get; init; }
    public Guid UserRoleGuid { get; init; }
    public string UserName { get; private set; }
    public string? UserEmail { get; private set; }
    public PhoneNumber? UserPhone { get; private set; }
    public string? UserAddress { get; private set; }
    public DateTime CreateDatetime { get; init; }
    public UserAccessFail UserAccessFail { get; init; }

    public ValueTask<User> ChangeByAddressAsync(ref string userAddress)
    {
        UserAddress = userAddress;
        return new ValueTask<User>(this);
    }

    /// <summary>
    ///     验证并设置密码
    /// </summary>
    /// <param name="hashPassword">密码</param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public ValueTask ChangeByPasswordAsync(ref string password)
    {
        if (password.Length <= 8)
        {
            throw new ArgumentOutOfRangeException("your are set password is short!");
        }

        var hash256Async = HashH256Tool.CreateHash256Async(password);
        PasswordHash = hash256Async.GetAwaiter().GetResult();
        return ValueTask.CompletedTask;
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
    ///     设置新密码
    /// </summary>
    /// <param name="phoneNumber"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public ValueTask ChangeByPhoneAsync(PhoneNumber phoneNumber)
    {
        if (phoneNumber.PhoneCode == UserPhone.PhoneCode) throw new ArgumentException("需要不要一样的号码");
        UserPhone = phoneNumber;
        return ValueTask.CompletedTask;
    }

    /// <summary>
    ///     验证密码
    /// </summary>
    /// <param name="hashPassword">密码哈希值</param>
    /// <returns></returns>
    public ValueTask<bool> CheckByPasswordAsync(string hashPassword)
    {
        return new ValueTask<bool>(PasswordHash == HashH256Tool.CreateHash256Async(hashPassword).Result);
    }

    // public ValueTask ChangeByHeadImageAsync(ref Uri imageUri)
    // {
    //     HeadImage = imageUri;
    //     return ValueTask.CompletedTask;
    // }
}