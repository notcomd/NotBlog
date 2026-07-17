using FileDev.Web.API.Application.Command;
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
        route.MapPost("/create_file_group", CreateFileGroupAsync)
            .WithHttpLogging(HttpLoggingFields.All, 1, 1);
        return route;
    }


    private static Task<IResult> UploadFileAsync(HttpContext httpContext, [FromServices] FileServicesDi servicesDi, [FromForm] FileStream stream,
        CancellationToken cancellationToken)
    {
        var userGuid=GetUserId(httpContext);
      
        var ext=Path.GetExtension(stream.Name).ToLowerInvariant();
        var fileType = ResolveFileType(ext);


        return Task.FromResult(Results.Json(new { ok = true }));
    }


    private static async Task<IResult> CreateFileGroupAsync(HttpContext httpContext, 
    [FromServices] FileServicesDi servicesDi, [FromBody] CreateFileGroupRequest request, CancellationToken cancellationToken)
    {
        var userGuid=GetUserId(httpContext);
       
        var command = new CreateNotFileGroupCommand()
        {
            UserGuid = userGuid.Value,
            FileGroupName = request.Name,
            FileGroupDescription = request.Description,
            FileGroupTags = request.GroupTags,
        };
        
        var identityCreateCommand=new IdentifiedCommand<CreateNotFileGroupCommand,bool>(Guid.CreateVersion7(),command);

        await servicesDi.NotMediator.SendAsync(identityCreateCommand, cancellationToken);

        return Results.Json(new { ok = true });
    }


    /// <summary>
    /// 获取用户ID
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    /// <returns>用户ID</returns>
    private static Guid? GetUserId(HttpContext context)
    {
        var claim = context.User.Claims.FirstOrDefault(x => x.Type == "id")
            ?? context.User.Claims.FirstOrDefault(x =>
                x.Type == System.Security.Claims.ClaimTypes.NameIdentifier);

        if (claim == null || !Guid.TryParse(claim.Value, out var userId))
            return null;
        return userId;
    }

    /// <summary>
    /// 解析文件类型
    /// </summary>
    /// <param name="ext">文件扩展名</param>
    /// <returns>文件类型</returns>
    private static FileType ResolveFileType(string ext) => ext switch
    {
        ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".webp" or ".svg" or ".ico" => FileType.FileImage,
        ".mp4" or ".avi" or ".mkv" or ".mov" or ".wmv" or ".flv" or ".webm" => FileType.FileVideo,
        ".mp3" or ".wav" or ".ogg" or ".flac" or ".aac" or ".wma" or ".m4a" => FileType.FileAudio,
        ".zip" or ".rar" or ".7z" or ".tar" or ".gz" or ".bz2" => FileType.CompressFiles,
        _ => FileType.FileFile
    };

}