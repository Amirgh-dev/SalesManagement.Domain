namespace SalesManagement.Application.Common.Interfaces
{
    public interface ITokenGenerator
    {
        int AccessTokenExpirationSeconds { get; }
        string GenerateAccessToken(int partyId, string phone);
    }
}