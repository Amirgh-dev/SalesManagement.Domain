namespace SalesManagement.Domain.Features.Auth.PartyLogin
{
    public interface IRevokedTokenStore
    {
        Task RevokeAsync(Guid jti, DateTimeOffset expiresAt, CancellationToken cancellationToken = default);

        Task<bool> IsRevokedAsync(Guid jti, CancellationToken cancellationToken = default);
    }
}
