namespace SalesManagement.Application.Features.Auth.PartyLogin
{
    public record PartyLoginResult(
        string AccessToken,
        int ExpiresInSeconds,
        int PartyId,
        string FullName);
}