using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Identity.Domain.Server
{
    public class IdentityDomainUserManagerServer
    {

        private readonly IUserRepository _userRepository;
        private readonly IUserRoleRepository _userRoleRepository;
        private readonly ILogger<IdentityDomainUserManagerServer> _logger;
        private readonly INotDateTime.INotDateTime _notDateTime;

        public IdentityDomainUserManagerServer(IUserRepository userRepository, IUserRoleRepository userRoleRepository,
            ILogger<IdentityDomainUserManagerServer> logger, INotDateTime.INotDateTime notDateTime)
        {
            _userRepository = userRepository;
            _userRoleRepository = userRoleRepository;
            _logger = logger;
            _notDateTime = notDateTime;
        }

        // Other methods for managing users can be added here
    }
}
