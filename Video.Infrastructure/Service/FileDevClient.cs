using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace Video.Infrastructure.Service;

/// <summary>
/// HTTP client for calling FileDev.Web.API file storage service
/// </summary>
public class FileDevClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FileDevClient> _logger;

    public FileDevClient(HttpClient httpClient, ILogger<FileDevClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<FileUploadResult> UploadVideoAsync(
        Stream fileStream, string fileName, Guid userId,
        CancellationToken cancellationToken = default)
    {
        using var content = new MultipartFormDataContent();

        var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        content.Add(streamContent, "file", fileName);

        var url = $"/api/external/file/save?userId={userId}&fileIdentity=FilePrivate";

        var response = await _httpClient.PostAsync(url, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<FileUploadResult>(cancellationToken: cancellationToken);

        if (result is null || !result.Success)
            throw new InvalidOperationException($"File upload failed: {result?.ErrorMessage ?? "Unknown error"}");

        _logger.LogInformation("Video file uploaded: {FileName} -> {FileUri}", fileName, result.FileUri);

        return result;
    }
}

public record FileUploadResult
{
    public bool Success { get; init; }
    public string FileUri { get; init; } = string.Empty;
    public string FileMd5 { get; init; } = string.Empty;
    public long FileSize { get; init; }
    public string? ErrorMessage { get; init; }
}
