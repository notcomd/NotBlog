namespace Message.Web.API.Application.Queries.Community;

/// <summary>话题帖子流查询处理程序。</summary>
public class GetTopicPostsQueryHandler(
    ITweetRepository tweetRepository,
    ICircleRepository circleRepository,
    ITopicRepository topicRepository) : IRequestHandler<GetTopicPostsQuery, PagedResult<Tweet>>
{
    public async Task<PagedResult<Tweet>> Handler(GetTopicPostsQuery query, CancellationToken cancellationToken)
    {
        // R-12：话题不存在或已停用时帖子流返回空（历史数据不展示）
        var topic = await topicRepository.GetByIdAsync(query.TopicGuid);
        if (topic is null || !topic.IsActive)
            return new PagedResult<Tweet>
            {
                Items = [],
                TotalCount = 0,
                Page = query.Page,
                PageSize = query.PageSize
            };

        var items = (await tweetRepository.GetByTopicAsync(query.TopicGuid, query.Page, query.PageSize)).ToList();
        var total = await tweetRepository.GetTopicPostCountAsync(query.TopicGuid);

        // 圈子帖仅对圈子成员可见（作者本人可见），其余过滤
        var visible = new List<Tweet>(items.Count);
        foreach (var tweet in items)
        {
            if (tweet.CircleGuid is null
                || tweet.AuthorGuid == query.CurrentUserId
                || await circleRepository.IsMemberAsync(tweet.CircleGuid.Value, query.CurrentUserId))
            {
                visible.Add(tweet);
            }
        }

        return new PagedResult<Tweet>
        {
            Items = visible,
            TotalCount = total,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }
}
