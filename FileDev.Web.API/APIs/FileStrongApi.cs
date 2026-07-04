using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Mvc;

namespace FileDev.Web.API.APIs;

public static class FileStrongApi
{
    public static RouteGroupBuilder FileStrongApis(this RouteGroupBuilder routeGroupBuilder)
    {
        var route = routeGroupBuilder.MapGroup("/filestorage");
        route.MapPost("/upload_file", UploadFileAsync)
            .WithHttpLogging(HttpLoggingFields.All, 1, 1);
        return route;
    }


    private static Task<IResult> UploadFileAsync([FromServices] FileServicesDi servicesDi, [FromForm] FileStream stream,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(Results.Ok("ok"));
    }
}