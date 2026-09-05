namespace SalesManagement.Application.Features.Auth.PartyLogin
{
    public record PartyLoginResult(
        string AccessToken,
        string RefreshToken,
        int ExpiresInSeconds,
        int PartyId,
        string FullName);
}