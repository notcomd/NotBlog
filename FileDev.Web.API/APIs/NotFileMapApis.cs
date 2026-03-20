using FileDev.Domain.Entities;
using FileDev.Domain.IServices;

namespace FileDev.Web.API.APIs;

public static class NotFileMapApis
{
    
    public static RouteGroupBuilder NotFileApis(this RouteGroupBuilder routeGroupBuilder)
    {
        var router = routeGroupBuilder.MapGroup("/Notfile");

        router.MapGet("/findFile", GetFileByIdAsync);
        router.MapPost("/uploadFile", UpLoadFileAsync);
       return router;
    }
    
    
    private static async Task<IResult> GetFileByIdAsync(HttpContext context,INotFileService notFileService, Guid fileId)
    { 
        return  Results.Json(await notFileService.GetFileByIdAsync(fileId));
    }

    
    private static async Task<IResult> UpLoadFileAsync(HttpContext context, INotFileService notFileService,
        IFormFile file)
    {
       await notFileService.CreateFileAsync(Guid.Parse(context.User.Claims.First(x => x.Type == "id").Value),
            file.FileName, null, string.Empty, FileType.FileFile, file.Length, new Uri(file.OpenReadStream().ToString()! ),
            file.ContentType);
       return Results.Json(new {message = "上传成功"});
    }
    
}