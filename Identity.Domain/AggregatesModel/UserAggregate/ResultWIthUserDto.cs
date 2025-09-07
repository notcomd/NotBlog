namespace Identity.Domain.AggregatesModel.UserAggregate;

public class ResultWIthUserDto
{
    public ResultWIthUserDto(string userName, string userEmail, Uri imageCoveer, Guid roleGuid, WithResultPhoneDto? withResultPhoneDto,
        WIthResultSafetyDto wIthResultSafetyDto, IEnumerable<WithResultClaimDto> claims, DateTimeOffset createdTime)
    {
        UserName = userName;
        UserEmail = userEmail;
        ImageCoveer = imageCoveer;
        WithResultPhoneDto = withResultPhoneDto;
        WIthResultSafetyDto = wIthResultSafetyDto;
        CreatedTime = createdTime;
        Claims = claims;
        RoleGuid = roleGuid;
    }

    public string UserName { get; }

    public string UserEmail { get; }
                                                                     
    public Guid RoleGuid { get; }                     

    public Uri ImageCoveer { get; }

    public WithResultPhoneDto? WithResultPhoneDto { get; }

    public WIthResultSafetyDto WIthResultSafetyDto { get; }

    public IEnumerable<WithResultClaimDto> Claims { get; } = Enumerable.Empty<WithResultClaimDto>();

    public DateTimeOffset CreatedTime { get; }

}
