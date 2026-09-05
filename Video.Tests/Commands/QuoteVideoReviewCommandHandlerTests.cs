using Video.Tests.Commands;

namespace Video.Tests.Commands;

/// <summary>
/// 评论点赞/点踩命令处理器单元测试 — 覆盖互动实施文档 D-7：
/// upvote → ReviewQuote.Like；down → ReviewQuote.Dislike；Redis ReviewQuote 哈希增量；非法字段拒绝。
/// </summary>
[TestFixture]
public class QuoteVideoReviewCommandHandlerTests
{
    private static readonly Guid AuthorId = Guid.NewGuid();
    private static readonly Guid ActorId = Guid.NewGuid();
    private static readonly Guid VideoId = Guid.NewGuid();
    private static readonly Guid ReviewId = Guid.NewGuid();

    private Mock<IVideoRepository> _videoRepo = null!;
    private Mock<IVideoCacheService> _cache = null!;
    private QuoteVideoReviewCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        var video = VideoFixtures.BuildVideo(AuthorId, VideoId);
        VideoFixtures.AddReview(video, ReviewId, AuthorId, null, "被点赞的评论");
        (_videoRepo, _) = VideoFixtures.BuildVideoRepository(video);
        _cache = VideoFixtures.BuildCacheService();
        _handler = new QuoteVideoReviewCommandHandler(
            _videoRepo.Object, _cache.Object, NullLogger<QuoteVideoReviewCommandHandler>.Instance);
    }

    private Task<QuoteVideoReviewResult> HandleAsync(string field, bool isLike)
        => _handler.Handler(
            new QuoteVideoReviewCommand(Guid.CreateVersion7(), VideoId, ActorId, ReviewId, field, isLike),
            default);

    [Test]
    public async Task 点赞_计数递增并刷新Redis缓存()
    {
        var result = await HandleAsync("upvote", isLike: true);

        Assert.That(result.Success, Is.True);
        Assert.That(result.NewCount, Is.EqualTo(1));
        _cache.Verify(c => c.IncrementReviewQuoteFieldAsync(
            ReviewId, "like", 1L, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task 点踩_计数递增并刷新Redis缓存()
    {
        var result = await HandleAsync("down", isLike: true);

        Assert.That(result.Success, Is.True);
        Assert.That(result.NewCount, Is.EqualTo(1));
        _cache.Verify(c => c.IncrementReviewQuoteFieldAsync(
            ReviewId, "dislike", 1L, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task 取消点赞_计数递减()
    {
        await HandleAsync("upvote", isLike: true);   // Like = 1
        var result = await HandleAsync("upvote", isLike: false); // Like = 0

        Assert.That(result.NewCount, Is.EqualTo(0));
        _cache.Verify(c => c.IncrementReviewQuoteFieldAsync(
            ReviewId, "like", -1L, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task 非法字段_拒绝且不落库()
    {
        var result = await HandleAsync("share", isLike: true);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("Invalid field"));
        _videoRepo.Verify(v => v.UnitOfWork.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}