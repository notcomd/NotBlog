using DomainCommonst;
using Microsoft.Extensions.DependencyInjection;

namespace Notcomd.Token.JWT;

public class ModuleInitializer : IModuleInitializer
{

    public void Initialize(IServiceCollection service)
    {
        service.AddScoped<IJwtTokenService, JwtTokenService>();
    }
}