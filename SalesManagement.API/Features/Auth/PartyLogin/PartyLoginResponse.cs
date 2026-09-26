namespace SalesManagement.API.Features.Auth.PartyLogin
{
    public record PartyLoginResponse(
        string AccessToken, int ExpiresIn, int PartyId, string FullName, string? ProfilePhotoUrl = null);
}
