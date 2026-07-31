using Identity.Web.API.Application.Commands;
using Moq;

namespace Identity.Web.API.Tests;

public class UploadAvatarCommandHandlerTests
{
    private static readonly Guid TestUserId = Guid.Parse("12345678-1234-1234-1234-123456789abc");
    private static readonly byte[] ValidImageContent = CreateMinimalJpeg();

    /// <summary>
    /// 创建一个最小的有效 JPEG 字节数组用于测试
    /// </summary>
    private static byte[] CreateMinimalJpeg(int size = 1024)
    {
        var data = new byte[size];
        // JPEG SOI marker
        data[0] = 0xFF;
        data[1] = 0xD8;
        // Fill the rest with some data
        for (int i = 2; i < size; i++)
            data[i] = (byte)(i % 256);
        return data;
    }

    [Fact]
    public async Task Handler_EmptyContent_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var command = new UploadAvatarCommand(
            TestUserId, "avatar.jpg", Array.Empty<byte>(), "image/jpeg");

        // Act & Assert - validation happens before gRPC call, so null client is fine
        var handler = new UploadAvatarCommandHandler(null!, null!);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handler(command, CancellationToken.None));

        Assert.Contains("不能为空", ex.Message);
    }

    [Fact]
    public async Task Handler_ExceedsSizeLimit_ShouldThrowInvalidOperationException()
    {
        // Arrange - create content larger than 5MB
        var oversized = new byte[6 * 1024 * 1024];
        oversized[0] = 0xFF;
        oversized[1] = 0xD8;

        var command = new UploadAvatarCommand(
            TestUserId, "avatar.jpg", oversized, "image/jpeg");

        // Act & Assert
        var handler = new UploadAvatarCommandHandler(null!, null!);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handler(command, CancellationToken.None));

        Assert.Contains("超过限制", ex.Message);
    }

    [Fact]
    public async Task Handler_InvalidExtension_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var command = new UploadAvatarCommand(
            TestUserId, "avatar.gif", ValidImageContent, "image/gif");

        // Act & Assert
        var handler = new UploadAvatarCommandHandler(null!, null!);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handler(command, CancellationToken.None));

        Assert.Contains("不支持的图片格式", ex.Message);
        Assert.Contains(".gif", ex.Message);
    }

    [Fact]
    public async Task Handler_InvalidContentType_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var command = new UploadAvatarCommand(
            TestUserId, "avatar.png", ValidImageContent, "image/gif");

        // Act & Assert
        var handler = new UploadAvatarCommandHandler(null!, null!);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handler(command, CancellationToken.None));

        Assert.Contains("不支持的 Content-Type", ex.Message);
    }

    [Fact]
    public async Task Handler_JpegExtension_ShouldPassValidation()
    {
        // Arrange
        var command = new UploadAvatarCommand(
            TestUserId, "avatar.jpg", ValidImageContent, "image/jpeg");

        var handler = new UploadAvatarCommandHandler(null!, null!);

        // Act & Assert - validation passes, but gRPC call throws NullReferenceException
        // (the gRPC client is null, so it won't actually call the service)
        var ex = await Assert.ThrowsAsync<NullReferenceException>(
            () => handler.Handler(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handler_PngExtension_ShouldPassValidation()
    {
        // Arrange - ensure PNG header
        var pngContent = new byte[1024];
        pngContent[0] = 0x89;
        pngContent[1] = 0x50;
        pngContent[2] = 0x4E;
        pngContent[3] = 0x47;
        var command = new UploadAvatarCommand(
            TestUserId, "avatar.png", pngContent, "image/png");

        var handler = new UploadAvatarCommandHandler(null!, null!);

        // Act & Assert - validation passes, gRPC call fails
        var ex = await Assert.ThrowsAsync<NullReferenceException>(
            () => handler.Handler(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handler_WebpExtension_ShouldPassValidation()
    {
        // Arrange
        var command = new UploadAvatarCommand(
            TestUserId, "avatar.webp", ValidImageContent, "image/webp");

        var handler = new UploadAvatarCommandHandler(null!, null!);

        // Act & Assert - validation passes, gRPC call fails
        var ex = await Assert.ThrowsAsync<NullReferenceException>(
            () => handler.Handler(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handler_NoExtension_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var command = new UploadAvatarCommand(
            TestUserId, "avatar", ValidImageContent, "image/jpeg");

        var handler = new UploadAvatarCommandHandler(null!, null!);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handler(command, CancellationToken.None));

        Assert.Contains("不支持的图片格式", ex.Message);
    }

    [Fact]
    public async Task Handler_EmptyContentType_ShouldPassValidation()
    {
        // Arrange - empty content type should be treated as valid (not strictly enforced)
        var command = new UploadAvatarCommand(
            TestUserId, "avatar.jpg", ValidImageContent, "");

        var handler = new UploadAvatarCommandHandler(null!, null!);

        // Act & Assert - validation passes, gRPC call fails
        var ex = await Assert.ThrowsAsync<NullReferenceException>(
            () => handler.Handler(command, CancellationToken.None));
    }
}

public class UploadAvatarCommandTests
{
    [Fact]
    public void Command_ShouldImplementILoggableCommand()
    {
        // Arrange
        var command = new UploadAvatarCommand(
            Guid.NewGuid(), "test.jpg", new byte[] { 0xFF, 0xD8 }, "image/jpeg");

        // Act & Assert
        var loggable = (ILoggableCommand)command;
        Assert.Equal("UserId", loggable.IdProperty);
        Assert.Equal(command.UserId.ToString(), loggable.IdValue);
    }

    [Fact]
    public void Command_ShouldBeARecord_WithValueEquality()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var content = new byte[] { 0xFF, 0xD8 };
        var cmd1 = new UploadAvatarCommand(userId, "a.jpg", content, "image/jpeg");
        var cmd2 = new UploadAvatarCommand(userId, "a.jpg", content, "image/jpeg");

        // Act & Assert
        Assert.Equal(cmd1, cmd2);
    }

    [Fact]
    public void UploadAvatarResult_ShouldBeImmutable()
    {
        // Arrange
        var result = new UploadAvatarResult(
            "file-id", "/files/avatar.jpg", "abc123", 1024, 200, 200, "jpg");

        // Act & Assert
        Assert.Equal("file-id", result.FileId);
        Assert.Equal("/files/avatar.jpg", result.FileUri);
        Assert.Equal("abc123", result.FileMd5);
        Assert.Equal(1024, result.FileSize);
        Assert.Equal(200, result.Width);
        Assert.Equal(200, result.Height);
        Assert.Equal("jpg", result.Format);
    }
}
