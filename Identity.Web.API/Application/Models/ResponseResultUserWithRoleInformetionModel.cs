namespace Identity.Web.API.Application.Models;

public class ResponseResultUserWithRoleInformetionModel
{
    public ResponseResultUserWithRoleInformetionModel(ResultWithRoleDto resultWithRoleDto, ResultWIthUserDto resultWIthUserDto)
    {
        ResultWithRoleDto = resultWithRoleDto;
        ResultWIthUserDto = resultWIthUserDto;
    }

    public ResultWithRoleDto ResultWithRoleDto { get;}

    public ResultWIthUserDto ResultWIthUserDto{ get; }
}
