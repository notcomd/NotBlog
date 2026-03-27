using Message.Domain.Entities;
using Message.Domain.IProvider;
using Message.Domain.IServices;
using Message.Web.API.Dto;
using Message.Web.API.Dto.Response;
using Microsoft.AspNetCore.Mvc;

namespace Message.Web.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FilesController : ControllerBase
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IFileProvider _fileProvider;

    public FilesController(IFileProvider fileProvider, ICurrentUserService currentUserService)
    {
        _fileProvider = fileProvider;
        _currentUserService = currentUserService;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<FileAttachmentDto>>> UploadFile([FromBody] UploadFileRequest request)
    {
        var attachment = await _fileProvider.UploadFileAsync(
            request.MessageId,
            request.FileName,
            request.FileType,
            request.FileSize,
            new Uri(request.FileUrl));

        return Ok(ApiResponse<FileAttachmentDto>.Created(MapToDto(attachment), "文件上传成功"));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<FileAttachmentDto>>> GetFile(Guid id)
    {
        var file = await _fileProvider.GetFileAsync(id);
        if (file == null)
            return NotFound(ApiResponse<FileAttachmentDto>.NotFound("文件不存在"));

        return Ok(ApiResponse<FileAttachmentDto>.Ok(MapToDto(file)));
    }

    [HttpGet("{id}/download")]
    public async Task<ActionResult> DownloadFile(Guid id)
    {
        var file = await _fileProvider.GetFileAsync(id);
        if (file == null)
            return NotFound();

        await _fileProvider.RecordDownloadAsync(id);

        return Redirect(file.FileUri.ToString());
    }

    [HttpGet("{id}/preview")]
    public async Task<ActionResult<ApiResponse<FileAttachmentDto>>> GetPreview(Guid id)
    {
        var file = await _fileProvider.GetFileAsync(id);
        if (file == null)
            return NotFound(ApiResponse<FileAttachmentDto>.NotFound("文件不存在"));

        if (!_fileProvider.IsImage(file.FileType))
            return BadRequest(ApiResponse<FileAttachmentDto>.BadRequest("该文件不支持预览"));

        return Ok(ApiResponse<FileAttachmentDto>.Ok(MapToDto(file)));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse>> DeleteFile(Guid id)
    {
        await _fileProvider.DeleteFileAsync(id);
        return Ok(ApiResponse.Ok("文件已删除"));
    }

    [HttpGet("message/{messageId}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<FileAttachmentDto>>>> GetMessageFiles(Guid messageId)
    {
        var files = await _fileProvider.GetMessageFilesAsync(messageId);
        return Ok(ApiResponse<IEnumerable<FileAttachmentDto>>.Ok(files.Select(MapToDto)));
    }

    [HttpGet("{id}/exists")]
    public async Task<ActionResult<ApiResponse<bool>>> FileExists(Guid id)
    {
        var exists = await _fileProvider.FileExistsAsync(id);
        return Ok(ApiResponse<bool>.Ok(exists));
    }

    [HttpGet("{id}/download-count")]
    public async Task<ActionResult<ApiResponse<int>>> GetDownloadCount(Guid id)
    {
        var count = await _fileProvider.GetDownloadCountAsync(id);
        return Ok(ApiResponse<int>.Ok(count));
    }

    [HttpGet("{id}/size")]
    public async Task<ActionResult<ApiResponse<string>>> GetFileSize(Guid id)
    {
        var file = await _fileProvider.GetFileAsync(id);
        if (file == null)
            return NotFound(ApiResponse<string>.NotFound("文件不存在"));

        var formattedSize = _fileProvider.GetFormattedFileSize(file.FileSize);
        return Ok(ApiResponse<string>.Ok(formattedSize));
    }

    [HttpGet("{id}/type")]
    public async Task<ActionResult<ApiResponse<FileTypeInfo>>> GetFileType(Guid id)
    {
        var file = await _fileProvider.GetFileAsync(id);
        if (file == null)
            return NotFound(ApiResponse<FileTypeInfo>.NotFound("文件不存在"));

        var typeInfo = new FileTypeInfo
        {
            FileType = file.FileType,
            IsImage = _fileProvider.IsImage(file.FileType),
            IsVideo = _fileProvider.IsVideo(file.FileType),
            IsAudio = _fileProvider.IsAudio(file.FileType),
            IsDocument = _fileProvider.IsDocument(file.FileType)
        };

        return Ok(ApiResponse<FileTypeInfo>.Ok(typeInfo));
    }

    private static FileAttachmentDto MapToDto(FileAttachment file) => new()
    {
        AttachmentId = file.AttachmentId,
        FileName = file.FileName,
        FileType = file.FileType,
        FileSize = file.FileSize,
        FileUrl = file.FileUri.ToString(),
        ThumbnailUrl = file.ThumbnailUri?.ToString(),
        UploadTime = file.UploadTime,
        DownloadCount = file.DownloadCount
    };

    public record UploadFileRequest(Guid MessageId, string FileName, string FileType, long FileSize, string FileUrl);

    public class FileTypeInfo
    {
        public string FileType { get; init; } = string.Empty;
        public bool IsImage { get; init; }
        public bool IsVideo { get; init; }
        public bool IsAudio { get; init; }
        public bool IsDocument { get; init; }
    }
}