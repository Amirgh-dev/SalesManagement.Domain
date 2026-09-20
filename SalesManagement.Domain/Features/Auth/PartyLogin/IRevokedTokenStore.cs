namespace SalesManagement.Domain.Features.Auth.PartyLogin
{
    public interface IRevokedTokenStore
    {
        Task RevokeAsync(string jti, DateTimeOffset expiresAt, CancellationToken cancellationToken = default);

        Task<bool> IsRevokedAsync(string jti, CancellationToken cancellationToken = default);
    }
}
