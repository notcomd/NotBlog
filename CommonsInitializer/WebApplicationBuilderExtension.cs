using Microsoft.Extensions.DependencyInjection;

namespace DomainCommonst;

public static class WebApplicationBuilderExtension
{
    public static IServiceCollection AddDomainCommonst(this IServiceCollection service)
    {


        return service;
    }
}