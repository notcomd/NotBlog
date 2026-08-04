using Message.Domain.Entities.Tweet;
using Message.Domain.Enums;
using Message.Domain.IRepository;
using Commons.SeedWork;
using Message.Web.API.Application.Commands.Reports;
using Microsoft.Extensions.Logging;
using Moq;

namespace Message.Tests.Commands.Reports;

/// <summary>
/// 提交举报命令处理程序单元测试。
/// 验证：以正确的参数创建举报，并仅返回操作结果（bool）。
/// </summary>
[TestFixture]
public class SubmitReportCommandHandlerTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid TargetGuid = Guid.NewGuid();

    private Mock<ITweetReportRepository> _reportRepository = null!;
    private Mock<ITweetRepository> _tweetRepository = null!;
    private Mock<ICommentRepository> _commentRepository = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;

    [SetUp]
    public void Setup()
    {
        _reportRepository = new Mock<ITweetReportRepository>();
        _tweetRepository = new Mock<ITweetRepository>();
        _commentRepository = new Mock<ICommentRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _reportRepository.SetupGet(r => r.UnitOfWork).Returns(_unitOfWork.Object);
    }

    [Test]
    public async Task SubmitReport_举报推文_应创建举报并返回true()
    {
        var tweet = Tweet.Create(UserId, "被举报的推文");
        _tweetRepository.Setup(r => r.GetByIdAsync(TargetGuid)).ReturnsAsync(tweet);

        TweetReport? added = null;
        _reportRepository.Setup(r => r.AddAsync(It.IsAny<TweetReport>()))
            .Callback<TweetReport>(t => added = t)
            .ReturnsAsync((TweetReport t) => t);

        var evidence = new[] { "https://example.com/evidence.png" };
        var handler = new SubmitReportCommandHandler(
            _reportRepository.Object,
            _tweetRepository.Object,
            _commentRepository.Object,
            new Mock<ILogger<SubmitReportCommandHandler>>().Object);

        var result = await handler.Handler(
            new SubmitReportCommand(UserId, "Tweet", TargetGuid, "广告内容", "Spam", evidence),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(added, Is.Not.Null);
            Assert.That(added!.ReportReason, Is.EqualTo("广告内容"));
            Assert.That(added.TargetType, Is.EqualTo(ReportTargetType.Tweet));
            Assert.That(added.TargetGuid, Is.EqualTo(TargetGuid));
            Assert.That(added.ReportedUserGuid, Is.EqualTo(UserId));
            _tweetRepository.Verify(r => r.GetByIdAsync(TargetGuid), Times.Once);
            _reportRepository.Verify(r => r.AddAsync(It.IsAny<TweetReport>()), Times.Once);
            _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        });
    }

    [Test]
    public async Task SubmitReport_被举报推文不存在时_应抛出KeyNotFoundException()
    {
        _tweetRepository.Setup(r => r.GetByIdAsync(TargetGuid)).ReturnsAsync((Tweet?)null);

        var handler = new SubmitReportCommandHandler(
            _reportRepository.Object,
            _tweetRepository.Object,
            _commentRepository.Object,
            new Mock<ILogger<SubmitReportCommandHandler>>().Object);

        Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            await handler.Handler(
                new SubmitReportCommand(UserId, "Tweet", TargetGuid, "广告内容", "Spam", null),
                CancellationToken.None));

        _reportRepository.Verify(r => r.AddAsync(It.IsAny<TweetReport>()), Times.Never);
    }
}
