using System.ComponentModel.DataAnnotations;
using FileDev.Domain.Entities;
using FileDev.Domain.IServices;
using FileDev.Infrastructure.Service;
using Microsoft.AspNetCore.Mvc;

namespace FileDev.Web.API.APIs;

public static class NotFileMapApis
{
    public static RouteGroupBuilder NotFileApis(this RouteGroupBuilder routeGroupBuilder)
    {
        var router = routeGroupBuilder.MapGroup("/Notfile");

        router.MapGet("/findFile", GetFileByIdAsync)
            .WithMetadata(new IgnoreAntiforgeryTokenAttribute())
            .WithMetadata(new DisableRequestSizeLimitAttribute());

        router.MapPost("/uploadFile", UpLoadFileAsync).WithMetadata(new IgnoreAntiforgeryTokenAttribute())
            .WithMetadata(new DisableRequestSizeLimitAttribute())
            ;

        router.MapPost("/getFileChunk", GetFileChunkAsync).WithMetadata(new IgnoreAntiforgeryTokenAttribute())
            .WithMetadata(new DisableRequestSizeLimitAttribute())
            ;

        return router;
    }


    private static async Task<IResult> GetFileByIdAsync(HttpContext context,
        [FromServices] INotFileService notFileService,
        [Required] Guid fileId)
    {
        return Results.Json(await notFileService.GetFileByIdAsync(fileId));
    }


    private static async Task<IResult> UpLoadFileAsync(HttpContext context,
        [FromServices] INotFileService notFileService,
        [Required] IFormFile file)
    {
        await notFileService.CreateFileAsync(Guid.Parse(context.User.Claims.First(x => x.Type == "id").Value),
            file.FileName, null, string.Empty, FileType.FileFile, file.Length,
            new Uri(file.OpenReadStream().ToString()!),
            file.ContentType);
        return Results.Json(new { message = "上传成功" });
    }

    private static async Task<IResult> GetFileChunkAsync(HttpContext context,
        [FromServices] INotFileStorageService notFileStorageService,
        FileStorageService fileStorageService,
        [FromForm, Required] IFormFile file, CancellationToken cancellationToken = default)
    {
        var response =
            await fileStorageService.GetTotalChunkCountAsync(file.Length);
        return Results.Json(response);
    }
}