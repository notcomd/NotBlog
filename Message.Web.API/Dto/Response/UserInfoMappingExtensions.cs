namespace Message.Web.API.Dto.Response;

/// <summary>用户资料实体 → DTO 映射。</summary>
public static class UserInfoMappingExtensions
{
    public static UserInfoDto ToDto(this UserInfo userInfo) => new()
    {
        UserId = userInfo.UserId,
        Level = userInfo.Level,
        Coins = userInfo.Coins,
        Experience = userInfo.Experience,
        BackgroundCoverUrl = userInfo.BackgroundCoverUrl,
        UpdateTime = userInfo.UpdateTime
    };
}
