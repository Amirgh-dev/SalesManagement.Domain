namespace SalesManagement.Domain.Features.Auth.PartyLogin
{
    public interface IPartyRepository
    {
        Task<Party?> GetByPhoneAndNationalCodeAsync(
            string phone, string nationalCode, CancellationToken cancellationToken = default);

        Task<Party?> GetByIdAsync(int partyId, CancellationToken cancellationToken = default);
    }
}
