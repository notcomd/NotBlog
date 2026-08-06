using Message.Domain.Entities;
using Message.Domain.Enums;
using Message.Domain.IRepository;
using Message.Domain.IServices;
using Commons.SeedWork;
using Message.Web.API.Application.Commands.Files;
using Message.Web.API.Dto.Response;
using Message.Web.API.Grpc;
using Microsoft.Extensions.Logging;
using Moq;
using MessageEntity = Message.Domain.Entities.Message;

namespace Message.Tests.Commands.Files;

/// <summary>
/// 文件 IDOR 负向测试（S-05）。
/// 覆盖：非上传者且非会话成员不得删除/记录下载文件；上传者与会话成员操作正常通过。
/// </summary>
[TestFixture]
public class FileIdorTests
{
    private static readonly Guid SessionId = Guid.NewGuid();
    private static readonly Guid SenderId = Guid.NewGuid();
    private static readonly Guid MemberId = Guid.NewGuid();
    private static readonly Guid OutsiderId = Guid.NewGuid();
    private static readonly Uri FileUri = new("https://example.com/file.txt");

    private Mock<IFileAttachmentRepository> _fileRepository = null!;
    private Mock<IMessageRepository> _messageRepository = null!;
    private Mock<IChatSessionRepository> _sessionRepository = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private Mock<ICurrentUserService> _currentUser = null!;
    private Mock<IFileStorageGrpcClient> _fileStorageMock = null!;

    private FileAttachment _file = null!;

    [SetUp]
    public void Setup()
    {
        _fileRepository = new Mock<IFileAttachmentRepository>();
        _messageRepository = new Mock<IMessageRepository>();
        _sessionRepository = new Mock<IChatSessionRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _currentUser = new Mock<ICurrentUserService>();

        // 文件属于 SenderId 发送的消息，会话参与者为 SenderId + MemberId
        var message = MessageEntity.CreateFileMessage(SessionId, SenderId, FileUri, "a.txt", 1024, "text/plain");
        _file = new FileAttachment(message.MessageId, Guid.NewGuid(), "a.txt", "text/plain", 1024, FileUri);

        _fileRepository.Setup(r => r.GetByIdAsync(_file.AttachmentId)).ReturnsAsync(_file);
        _fileRepository.Setup(r => r.UpdateAsync(It.IsAny<FileAttachment>()))
            .ReturnsAsync((FileAttachment f) => f);
        _messageRepository.Setup(r => r.GetByIdAsync(message.MessageId)).ReturnsAsync(message);
        _fileStorageMock = new Mock<IFileStorageGrpcClient>();
        _fileStorageMock.Setup(s => s.DeleteFileAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteFileResult(true, null));
        _sessionRepository.Setup(r => r.GetByIdAsync(SessionId))
            .ReturnsAsync(new ChatSession(SessionType.Private, SenderId,
                new HashSet<Guid> { SenderId, MemberId }));
    }

    private void ActingAs(Guid userId) => _currentUser.Setup(c => c.GetUserId()).Returns(userId);

    // ---------- DeleteFileCommandHandler ----------

    [Test]
    public async Task DeleteFile_非上传者非成员删除_应抛出UnauthorizedAccessException()
    {
        ActingAs(OutsiderId);

        var handler = new DeleteFileCommandHandler(
            _fileRepository.Object, _messageRepository.Object, _sessionRepository.Object,
            _currentUser.Object, _fileStorageMock.Object, _unitOfWork.Object,
            new Mock<ILogger<DeleteFileCommandHandler>>().Object);

        Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await handler.Handler(new DeleteFileCommand(_file.AttachmentId), CancellationToken.None));

        Assert.That(_file.IsDeleted, Is.False);
        _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task DeleteFile_上传者删除自己的文件_应返回true()
    {
        ActingAs(SenderId);

        var handler = new DeleteFileCommandHandler(
            _fileRepository.Object, _messageRepository.Object, _sessionRepository.Object,
            _currentUser.Object, _fileStorageMock.Object, _unitOfWork.Object,
            new Mock<ILogger<DeleteFileCommandHandler>>().Object);

        var result = await handler.Handler(new DeleteFileCommand(_file.AttachmentId), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(_file.IsDeleted, Is.True);
            _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        });
    }

    [Test]
    public async Task DeleteFile_会话成员删除_应返回true()
    {
        ActingAs(MemberId);

        var handler = new DeleteFileCommandHandler(
            _fileRepository.Object, _messageRepository.Object, _sessionRepository.Object,
            _currentUser.Object, _fileStorageMock.Object, _unitOfWork.Object,
            new Mock<ILogger<DeleteFileCommandHandler>>().Object);

        var result = await handler.Handler(new DeleteFileCommand(_file.AttachmentId), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(_file.IsDeleted, Is.True);
        });
    }

    // ---------- RecordDownloadCommandHandler ----------

    [Test]
    public async Task RecordDownload_非上传者非成员下载_应抛出UnauthorizedAccessException()
    {
        ActingAs(OutsiderId);

        var handler = new RecordDownloadCommandHandler(
            _fileRepository.Object, _messageRepository.Object, _sessionRepository.Object,
            _currentUser.Object, _unitOfWork.Object,
            new Mock<ILogger<RecordDownloadCommandHandler>>().Object);

        Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await handler.Handler(new RecordDownloadCommand(_file.AttachmentId), CancellationToken.None));

        Assert.That(_file.DownloadCount, Is.EqualTo(0));
        _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task RecordDownload_上传者下载自己的文件_应记录下载次数()
    {
        ActingAs(SenderId);

        var handler = new RecordDownloadCommandHandler(
            _fileRepository.Object, _messageRepository.Object, _sessionRepository.Object,
            _currentUser.Object, _unitOfWork.Object,
            new Mock<ILogger<RecordDownloadCommandHandler>>().Object);

        var result = await handler.Handler(new RecordDownloadCommand(_file.AttachmentId), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(_file.DownloadCount, Is.EqualTo(1));
            _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        });
    }
}
