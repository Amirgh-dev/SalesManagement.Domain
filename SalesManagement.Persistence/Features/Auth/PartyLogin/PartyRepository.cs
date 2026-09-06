using Dapper;
using SalesManagement.Application.Common.Interfaces;
using SalesManagement.Domain.Entities;
using SalesManagement.Domain.Features.Auth.PartyLogin;

namespace SalesManagement.Persistence.Features.Auth.PartyLogin
{
    public class PartyRepository : IPartyRepository
    {
        private readonly ISepidarConnectionFactory _connectionFactory;

        public PartyRepository(ISepidarConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<Party?> GetByPhoneAndNationalCodeAsync(
            string phone, string nationalCode, CancellationToken cancellationToken = default)
        {
            const string sql = 
                "SELECT DISTINCT GNR.Party.PartyId, GNR.Party.Name, GNR.Party.LastName, GNR.Party.IdentificationCode, GNR.Party.Email, GNR.Party.IsCustomer, GNR.Party.IsEmployee, GNR.Party.IsVendor, GNR.Party.IsInBlacklist, GNR.Party.CustomerRemaining, ACC.DL.Code AS DlCode, ACC.DL.Title AS DlTitle FROM GNR.Party INNER JOIN ACC.DL ON ACC.DL.DLId = GNR.Party.DLRef INNER JOIN GNR.PartyPhone ON GNR.Party.PartyId = GNR.PartyPhone.PartyRef WHERE GNR.PartyPhone.Type = 2 AND GNR.PartyPhone.Phone = @Phone AND GNR.Party.IdentificationCode = @NationalCode;";

            using var connection = _connectionFactory.CreateConnection();

            var commandDefinition = new CommandDefinition(sql, new
            {
                Phone = phone,
                NationalCode = nationalCode
            }, cancellationToken: cancellationToken);

            var dto = await connection.QueryFirstOrDefaultAsync<PartyLoginDto>(commandDefinition);

            if (dto is null)
                return null;

            return Party.Reconstitute(
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
}