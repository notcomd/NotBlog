namespace Identity.Web.API.Application.Commands;

public class CreateByRoleCommandHandler(IUserRoleRepository userRoleRepository)
    : NotMediator.IRequestHandler<CreateByRoleCommand, bool>
{
    public async Task<bool> Handler(CreateByRoleCommand request, CancellationToken cancellationToken)
    {
        var data = await userRoleRepository.FindByUserRoleAsync();

        return true;
    }
}