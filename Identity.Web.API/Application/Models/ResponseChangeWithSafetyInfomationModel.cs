namespace Identity.Web.API.Application.Models;

public class ResponseChangeWithSafetyInfomationModel
{

    public ResponseChangeWithSafetyInfomationModel(EnumBlackOrWhite enumBlackOrWhite, EnumUserStatus enumUserStatus, 
        DateTimeOffset lockTimeEd)
    {
        EnumBlackOrWhite = enumBlackOrWhite;
        EnumUserStatus = enumUserStatus;
        LockTimeEd = lockTimeEd;
    }

    public EnumBlackOrWhite EnumBlackOrWhite { get; }

    public EnumUserStatus EnumUserStatus { get; }

    public DateTimeOffset LockTimeEd { get; }

}
