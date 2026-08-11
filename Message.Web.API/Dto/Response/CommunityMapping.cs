namespace Message.Web.API.Dto.Response;

/// <summary>
/// 社区 DTO 映射扩展
/// </summary>
public static class CommunityMapping
{
    public static CircleDto ToDto(this Circle circle, Guid? viewerGuid = null)
    {
        var member = viewerGuid.HasValue ? circle.GetActiveMember(viewerGuid.Value) : null;
        return new CircleDto
        {
            CircleGuid = circle.CircleGuid,
            OwnerGuid = circle.OwnerGuid,
            Name = circle.Name,
            Description = circle.Description,
            AvatarUrl = circle.AvatarUrl,
            CoverUrl = circle.CoverUrl,
            MemberCount = circle.MemberCount,
            MaxMembers = circle.MaxMembers,
            Status = circle.Status.ToString(),
            CreateTime = circle.CreateTime,
            MyRole = member?.Role.ToString(),
            IsMember = member is not null
        };
    }

    public static CircleMemberDto ToDto(this CircleMember member) => new()
    {
        UserGuid = member.UserGuid,
        Role = member.Role.ToString(),
        Nickname = member.Nickname,
        JoinTime = member.JoinTime
    };

    public static CircleInvitationDto ToDto(this CircleInvitation invitation, string circleName) => new()
    {
        InviteGuid = invitation.InviteGuid,
        CircleGuid = invitation.CircleGuid,
        CircleName = circleName,
        InviterGuid = invitation.InviterGuid,
        InviteeGuid = invitation.InviteeGuid,
        Code = invitation.Code,
        Token = invitation.Type == CircleInvitationType.Link ? invitation.Token : null,
        Type = invitation.Type.ToString(),
        Status = invitation.Status.ToString(),
        ExpireTime = invitation.ExpireTime,
        CreateTime = invitation.CreateTime
    };

    public static TopicDto ToDto(this Topic topic) => new()
    {
        TopicGuid = topic.TopicGuid,
        Name = topic.Name,
        Description = topic.Description,
        PostCount = topic.PostCount,
        CreateTime = topic.CreateTime
    };

    public static CommunityPostDto ToCommunityDto(this Tweet tweet, bool isLiked = false, bool isFavorited = false) => new()
    {
        TweetGuid = tweet.TweetGuid,
        AuthorGuid = tweet.AuthorGuid,
        Content = tweet.Content,
        MediaUrls = tweet.Media.Any() ? tweet.Media.Select(m => m.MediaUrl).ToList() : null,
        TopicGuids = tweet.TopicGuids.Any() ? tweet.TopicGuids.ToList() : null,
        CircleGuid = tweet.CircleGuid,
        ViewCount = tweet.ViewCount,
        LikeCount = tweet.LikeCount,
        CommentCount = tweet.CommentCount,
        FavoriteCount = tweet.FavoriteCount,
        PublishTime = tweet.PublishTime ?? tweet.CreateTime,
        CreateTime = tweet.CreateTime,
        IsLiked = isLiked,
        IsFavorited = isFavorited
    };
}
