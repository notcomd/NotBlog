namespace Message.Web.API.Application.Queries.UserInfo;

/// <summary>
/// 签到日期查询处理程序：返回区间内签到日期（升序）+ 累计签到天数。
/// <para>只读查询，不落库；无签到记录时返回空集合。</para>
/// </summary>
public class GetSignInDatesQueryHandler(
    IUserSignInRepository signInRepository) : IRequestHandler<GetSignInDatesQuery, SignInDatesDto>
{
    public async Task<SignInDatesDto> Handler(GetSignInDatesQuery query, CancellationToken cancellationToken)
    {
        var dates = await signInRepository.GetDatesAsync(query.UserId, query.From, query.To);
        var totalDays = await signInRepository.CountAsync(query.UserId);

        return new SignInDatesDto
        {
            Dates = dates,
            TotalDays = totalDays
        };
    }
}
