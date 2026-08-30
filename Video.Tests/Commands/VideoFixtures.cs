using Commons.SeedWork;
using Notcomd.EventBus.Core;

namespace Video.Tests.Commands;

/// <summary>
/// 命令处理器测试共享基建：Videos 构造、仓储/缓存/总线/Redis Mock 装配。
/// </summary>
internal static class VideoFixtures
{
    /// <summary>构造带有作者与稳定 VideoGuid 的视频实体（VideoGuid 为 init 属性，对象初始化器赋值）</summary>
    public static Videos BuildVideo(Guid authorId, Guid videoId) => new(
        [authorId], "测试视频", new Uri("https://cover.example/1.jpg"),
        new Uri("https://files.example/1.mp4"), "测试简介", ["技术"])
    {
        VideoGuid = videoId
    };

    /// <summary>经由聚合根行为添加一条评论（与命令处理器写路径一致）</summary>
    public static void AddReview(Videos video, Guid reviewId, Guid userId, Guid? rootReview, string body)
        => video.AddByVideoReview(reviewId, userId, rootReview, body, null);

    /// <summary>装配 IVideoRepository（UnitOfWork + 指定视频查找）</summary>
    public static (Mock<IVideoRepository> VideoRepo, Mock<IUnitOfWork> UnitOfWork) BuildVideoRepository(Videos video)
    {
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var videoRepo = new Mock<IVideoRepository>();
        videoRepo.SetupGet(v => v.UnitOfWork).Returns(uow.Object);
        videoRepo.Setup(v => v.FindByVideoWithDetailsAsync(video.VideoGuid))
            .ReturnsAsync(video);
        return (videoRepo, uow);
    }

    /// <summary>装配 IVideoCacheService（全异步方法返回无副作用完成）</summary>
    public static Mock<IVideoCacheService> BuildCacheService()
    {
        var cache = new Mock<IVideoCacheService>();
        cache.Setup(c => c.RemoveVideoMetaAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        cache.Setup(c => c.InvalidateVideoListsAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        cache.Setup(c => c.InvalidateVideoReviewCachesAsync(It.IsAny<Guid>(), It.IsAny<IEnumerable<Guid>?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        cache.Setup(c => c.RemoveVideoReviewRepliesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        cache.Setup(c => c.IncrementReviewQuoteFieldAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0L);
        return cache;
    }

    /// <summary>装配 IEventBus（捕获发布的集成事件便于断言）</summary>
    public static (Mock<IEventBus> Bus, List<IntegrationEvent> Published) BuildEventBus()
    {
        var published = new List<IntegrationEvent>();
        var bus = new Mock<IEventBus>();
        bus.Setup(b => b.PublishAsync(It.IsAny<IntegrationEvent>()))
            .Callback<IntegrationEvent>(e => published.Add(e))
            .Returns(Task.CompletedTask);
        return (bus, published);
    }

    /// <summary>装配 IRedisCacheService（StringSetIfNotExistsAsync 默认首次成功）</summary>
    public static Mock<IRedisCacheService> BuildRedis(bool firstTime = true)
    {
        var redis = new Mock<IRedisCacheService>();
        redis.Setup(r => r.StringSetIfNotExistsAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(firstTime);
        return redis;
    }
}