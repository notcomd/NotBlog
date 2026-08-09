namespace Message.Web.API.Dto.Response;

/// <summary>签到结果（经验/等级/硬币/升级级数）。</summary>
public record SignInResultDto(int Level, long Experience, long Coins, int UpgradedLevels);
