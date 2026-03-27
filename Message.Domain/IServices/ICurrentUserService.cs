namespace Message.Domain.IServices;

public interface ICurrentUserService
{
    bool IsAuthenticated { get; }
    Guid GetUserId();

    string? GetUserRole();

    string? GetClaim(string claimType);
}