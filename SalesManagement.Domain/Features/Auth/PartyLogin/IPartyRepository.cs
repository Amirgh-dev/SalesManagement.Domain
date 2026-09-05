using SalesManagement.Domain.Entities;

namespace SalesManagement.Domain.Features.Auth.PartyLogin
{
    public interface IPartyRepository
    {
        Task<Party?> GetByPhoneAndNationalCodeAsync(
            string phone, string nationalCode, CancellationToken cancellationToken = default);
    }
}