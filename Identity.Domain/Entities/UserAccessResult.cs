namespace Identity.Domain.Entities;

public class UserAccessResult<T> where T : class
{
    public UserAccess UserAccess;

    public UserAccessResult(UserAccess userAccess, T retData, string message)
    {
        UserAccess = userAccess;
        RetData = retData;
        Message = message;
    }

    public T RetData { get; set; }

    public string Message { get; set; }
}