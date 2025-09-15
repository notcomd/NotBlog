namespace Identity.Web.API.Application.Models;

public class ResponseChangeWithSafetyInfomationModel
{

<<<<<<< HEAD
    public ResponseChangeWithSafetyInfomationModel(EnBlackOrWhite enumBlackOrWhite, EnUserStatus enumUserStatus, 
=======
    public ResponseChangeWithSafetyInfomationModel(EnumBlackOrWhite enumBlackOrWhite, EnumUserStatus enumUserStatus, 
>>>>>>> 8e1a7f66420ec3bdbf7689044ea9f7d83b5d42f9
        DateTimeOffset lockTimeEd)
    {
        EnumBlackOrWhite = enumBlackOrWhite;
        EnumUserStatus = enumUserStatus;
        LockTimeEd = lockTimeEd;
    }

<<<<<<< HEAD
    public EnBlackOrWhite EnumBlackOrWhite { get; }

    public EnUserStatus EnumUserStatus { get; }
=======
    public EnumBlackOrWhite EnumBlackOrWhite { get; }

    public EnumUserStatus EnumUserStatus { get; }
>>>>>>> 8e1a7f66420ec3bdbf7689044ea9f7d83b5d42f9

    public DateTimeOffset LockTimeEd { get; }

}
