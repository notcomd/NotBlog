namespace Message.Web.API.Dto.Response;

/// <summary>签到日期集合（供签到热力图渲染）。</summary>
public record SignInDatesDto
{
    /// <summary>查询区间内的签到日期（升序，格式 yyyy-MM-dd）</summary>
    public IReadOnlyList<DateOnly> Dates { get; init; } = [];

    /// <summary>累计签到天数（全部历史，不受查询区间限制）</summary>
    public int TotalDays { get; init; }
}
