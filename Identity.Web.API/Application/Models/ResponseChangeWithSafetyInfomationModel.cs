namespace Identity.Web.API.Application.Models;

public class ResponseChangeWithSafetyInfomationModel
{

    public ResponseChangeWithSafetyInfomationModel(EnBlackOrWhite enumBlackOrWhite, EnUserStatus enumUserStatus, 
        DateTimeOffset lockTimeEd)
    {
        EnumBlackOrWhite = enumBlackOrWhite;
        EnumUserStatus = enumUserStatus;
        LockTimeEd = lockTimeEd;
    }

    public EnBlackOrWhite EnumBlackOrWhite { get; }

    public EnUserStatus EnumUserStatus { get; }

    public DateTimeOffset LockTimeEd { get; }

}
