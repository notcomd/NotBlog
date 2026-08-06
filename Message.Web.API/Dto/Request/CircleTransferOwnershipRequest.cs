namespace Message.Web.API.Dto.Request;

/// <summary>转移圈主请求</summary>
public class CircleTransferOwnershipRequest
{
    public Guid NewOwnerGuid { get; init; }
}

