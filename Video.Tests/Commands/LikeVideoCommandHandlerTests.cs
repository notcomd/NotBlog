using Notcomd.EventBus.Core;
using Video.Tests.Commands;

namespace Video.Tests.Commands;

/// <summary>
/// 视频点赞命令处理器单元测试 — 覆盖互动实施文档 D-4/D-5：
/// 仅首次点赞/投币发布 VideoInteraction 通知；重复与取消不发；作者归属取 Affiliated 首个非操作者。
/// </summary>
[TestFixture]
public class LikeVideoCommandHandlerTests
{
    private static readonly Guid AuthorId = Guid.NewGuid();
    private static readonly Guid ActorId = Guid.NewGuid();
    private static readonly Guid VideoId = Guid.NewGuid();

    private Mock<IVideoRepository> _videoRepo = null!;
    private Mock<IVideoCacheService> _cache = null!;
    private Mock<IRedisCacheService> _redis = null!;
    private Mock<IEventBus> _bus = null!;
    private List<IntegrationEvent> _published = null!;
    private LikeVideoCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        var video = VideoFixtures.BuildVideo(AuthorId, VideoId);
        (_videoRepo, _) = VideoFixtures.BuildVideoRepository(video);
        _cache = VideoFixtures.BuildCacheService();
        _redis = VideoFixtures.BuildRedis(firstTime: true);
        (_bus, _published) = VideoFixtures.BuildEventBus();
        _handler = new LikeVideoCommandHandler(
            _videoRepo.Object, _cache.Object, _bus.Object, _redis.Object,
            NullLogger<LikeVideoCommandHandler>.Instance);
    }

    private Task<LikeVideoResult> HandleAsync(string field, bool isLike, Guid? actor = null)
        => _handler.Handler(new LikeVideoCommand(Guid.CreateVersion7(), VideoId, actor ?? ActorId, field, isLike), default);

    private VideoInteractionIntegrationEvent? SingleInteraction()
        => _published.OfType<VideoInteractionIntegrationEvent>().SingleOrDefault();

    /// <summary>默认 Setup 中 Redis 返回首次成功：重复场景下覆盖为已存在</summary>
    private void RedisIsNotFirst()
        => _redis.Setup(r => r.StringSetIfNotExistsAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

    [Test]
    public async Task 首次点赞_发布VideoLiked通知给作者()
    {
        var result = await HandleAsync("upvote", isLike: true);

        Assert.That(result.Success, Is.True);
        var evt = SingleInteraction();
        Assert.That(evt, Is.Not.Null);
        Assert.That(evt!.InteractionType, Is.EqualTo(VideoInteractionType.VideoLiked));
        Assert.That(evt.TargetUserId, Is.EqualTo(AuthorId));
        Assert.That(evt.ActorUserId, Is.EqualTo(ActorId));
        Assert.That(evt.VideoName, Is.EqualTo("测试视频"));
    }

    [Test]
    public async Task 首次投币_发布VideoCoined通知给作者()
    {
        var result = await HandleAsync("ballot", isLike: true);

        Assert.That(result.Success, Is.True);
        var evt = SingleInteraction();
        Assert.That(evt, Is.Not.Null);
        Assert.That(evt!.InteractionType, Is.EqualTo(VideoInteractionType.VideoCoined));
        Assert.That(evt.TargetUserId, Is.EqualTo(AuthorId));
    }

    [Test]
    public async Task 重复点赞_不发布通知()
    {
        RedisIsNotFirst();

        await HandleAsync("upvote", isLike: true);

        Assert.That(_published, Is.Empty);
    }

    [Test]
    public async Task 取消点赞_不发布通知且不写去重键()
    {
        await HandleAsync("upvote", isLike: false);

        Assert.That(_published, Is.Empty);
        _redis.Verify(r => r.StringSetIfNotExistsAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task 点踩_不发布通知()
    {
        await HandleAsync("down", isLike: true);

        Assert.That(_published, Is.Empty);
    }

    [Test]
    public async Task 作者给自己点赞_不发布通知()
    {
        await HandleAsync("upvote", isLike: true, actor: AuthorId);

        Assert.That(_published, Is.Empty);
    }

    [Test]
    public async Task 非支持字段_拒绝()
    {
        var result = await HandleAsync("illegal", isLike: true);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("Invalid field"));
        Assert.That(_published, Is.Empty);
    }
}