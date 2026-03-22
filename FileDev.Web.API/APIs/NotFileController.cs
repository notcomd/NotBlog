using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using FileDev.Domain.Entities;
using FileDev.Domain.IServices;
using FileDev.Infrastructure.Service;
using Microsoft.AspNetCore.Mvc;

namespace FileDev.Web.API.APIs;

[ApiController]
[Route($"api/[controller]")]
public class NotFileController(IHttpContextAccessor httpContextAccessor) : ControllerBase
{
    [HttpGet("hello")]
    public string Hello()
    {
        return "hello";
    }

    [IgnoreAntiforgeryToken]
    [HttpGet("findFile")]
    public async Task<IResult> GetFileByIdAsync(
        [FromServices] INotFileService notFileService,
        [Required] Guid fileId)
    {
        return Results.Json(await notFileService.GetFileByIdAsync(fileId));
    }

    [IgnoreAntiforgeryToken]
    [HttpPost("uploadFile")]
    public async Task<IResult> UpLoadFileAsync(
        [FromServices] INotFileService notFileService,
        [Required] IFormFile file)
    {
        var context = httpContextAccessor.HttpContext;
        if (context == null) return Results.Problem("HTTP上下文不可用");

        var userIdClaim = context.User.Claims.FirstOrDefault(x => x.Type == "id")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Results.Problem("用户ID无效");

        await notFileService.CreateFileAsync(userId,
            file.FileName, null, string.Empty, FileType.FileFile, file.Length,
            new Uri(file.OpenReadStream().ToString()!),
            file.ContentType);
        return Results.Json(new { message = "上传成功" });
    }

    [IgnoreAntiforgeryToken]
    [HttpPost("getFileChunk")]
    public async Task<IResult> GetFileChunkAsync(
        [FromServices] INotFileStorageService notFileStorageService,
        [FromServices] FileStorageService fileStorageService,
        [Required] long file, CancellationToken cancellationToken = default)
    {
        var response =
            await fileStorageService.GetTotalChunkCountAsync(file);
        return Results.Json(JsonSerializer.Serialize(new Dictionary<string, object> { { "totalChunks", response } }));
    }


    [HttpPost("uploadFileChunk")]
    public async Task<IResult> UpLoadFileChunkAsync(
        [FromServices] INotFileStorageService notFileStorageService,
        [FromServices] FileStorageService fileStorageService,
        [Required] string file,
        [Required] int chunk,
        [Required] byte[] chunkContent,
        CancellationToken cancellationToken = default)
    {
        var response = await fileStorageService.UploadChunkAsync(file, chunk, chunkContent);
        return Results.Json(response);
    }
}