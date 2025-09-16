using Microsoft.AspNetCore.Mvc;

namespace FileDev.Web.API.APIs;

public static class FileDevManagerApi
{
    public static RouteGroupBuilder FileDevManagerAPI(this RouteGroupBuilder routeGroupBuilder)
    {
        var router = routeGroupBuilder.MapGroup("/Manager")
            .WithName("文件管理接口")
            .WithHttpLogging(Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.All);

        router.MapPost("/UpLoadWithFileAsync", UpLoadWithFileAsync)
            .DisableRequestTimeout()
            .WithHttpLogging(Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.All);

        return routeGroupBuilder;
    }

    private static ValueTask<string> UpLoadWithFileAsync([FromBody] Stream FileData, CancellationToken cancellation = default)
    {

        return new ValueTask<string>("ok");
    }

}
