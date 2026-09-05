namespace SalesManagement.Application.Common.Interfaces
{
    public interface ITokenGenerator
    {
        string GenerateAccessToken(int partyId, string phone);
        string GenerateRefreshToken();
    }
}