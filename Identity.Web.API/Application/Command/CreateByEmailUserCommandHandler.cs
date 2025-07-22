namespace Identity.Web.API.Application.Command;

public class CreateByEmailUserCommandHandler : IRequestHandler<CreateByEmailUserCommand, bool>
{
   

    private readonly IUserRepository _userRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly ILogger<CreateByEmailUserCommandHandler> _logger;

    public CreateByEmailUserCommandHandler(IUserRepository userRepository, IUserRoleRepository userRoleRepository, ILogger<CreateByEmailUserCommandHandler> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _userRoleRepository = userRoleRepository ?? throw new ArgumentNullException(nameof(userRoleRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

  

    public async Task<bool> Handler(CreateByEmailUserCommand request, CancellationToken cancellationToken)
    {
        //await _emailCodeSend.SendEmailCodeAsync(request.Email, request.Code.ToString());
        _logger.LogInformation($"[{DateTime.UtcNow}]Email Send! ");
        return true;
    }

  
}

public class CreateByUserIdentifiedCommandHandler : IdentifiedCommandHandler<CreateByEmailUserCommand, bool>
{
    public CreateByUserIdentifiedCommandHandler(IRequestManager requestManager,
    ILogger<CreateByUserIdentifiedCommandHandler> logger,
    INotMediator notMediator) : base(notMediator, requestManager, logger)
    {
    }
    public override bool CreateResultForDuplicateRequest()
    {
        return true;
    }
}