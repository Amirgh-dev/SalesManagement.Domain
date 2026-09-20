using Dapper;
using SalesManagement.Persistence.ConnectionFactories;
using SalesManagement.Domain.Features.Auth.PartyLogin;

namespace SalesManagement.Persistence.Features.Auth.PartyLogin
{
    public class PartyRepository : IPartyRepository
    {
        private const string SqlSelectParty =
            "SELECT DISTINCT GNR.Party.PartyId, GNR.Party.Name, GNR.Party.LastName, GNR.Party.IdentificationCode, GNR.Party.Email, GNR.Party.IsCustomer, GNR.Party.IsEmployee, GNR.Party.IsVendor, GNR.Party.IsInBlacklist, GNR.Party.CustomerRemaining, ACC.DL.Code AS DlCode, ACC.DL.Title AS DlTitle FROM GNR.Party INNER JOIN ACC.DL ON ACC.DL.DLId = GNR.Party.DLRef ";

        private const string SqlByPhoneAndNationalCode =
            SqlSelectParty +
            "INNER JOIN GNR.PartyPhone ON GNR.Party.PartyId = GNR.PartyPhone.PartyRef WHERE GNR.PartyPhone.Type = 2 AND GNR.PartyPhone.Phone = @Phone AND GNR.Party.IdentificationCode = @NationalCode;";

        private const string SqlById = SqlSelectParty + "WHERE GNR.Party.PartyId = @PartyId;";

        private readonly ISepidarConnectionFactory _connectionFactory;

        public PartyRepository(ISepidarConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<Party?> GetByPhoneAndNationalCodeAsync(
            string phone, string nationalCode, CancellationToken cancellationToken = default)
        {
            using var connection = _connectionFactory.CreateConnection();

            var dto = await connection.QueryFirstOrDefaultAsync<PartyLoginDto>(new CommandDefinition(
                SqlByPhoneAndNationalCode, new { Phone = phone, NationalCode = nationalCode },
                cancellationToken: cancellationToken));

            return dto is null ? null : ToParty(dto);
        }

        public async Task<Party?> GetByIdAsync(int partyId, CancellationToken cancellationToken = default)
        {
            using var connection = _connectionFactory.CreateConnection();

            var dto = await connection.QueryFirstOrDefaultAsync<PartyLoginDto>(new CommandDefinition(
                SqlById, new { PartyId = partyId }, cancellationToken: cancellationToken));

            return dto is null ? null : ToParty(dto);
        }

        private static Party ToParty(PartyLoginDto dto) => Party.Reconstitute(
            partyId: dto.PartyId,
            name: dto.Name,
            lastName: dto.LastName,
            identificationCode: dto.IdentificationCode,
            email: dto.Email,
            isCustomer: dto.IsCustomer,
            isEmployee: dto.IsEmployee,
            isVendor: dto.IsVendor,
            isInBlacklist: dto.IsInBlacklist,
            customerRemaining: dto.CustomerRemaining,
            dlCode: dto.DlCode,
            dlTitle: dto.DlTitle);
    }
}
