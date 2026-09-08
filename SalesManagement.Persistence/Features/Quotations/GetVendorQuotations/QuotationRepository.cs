using Dapper;
using SalesManagement.Application.Common.Interfaces;
using SalesManagement.Domain.Entities;
using SalesManagement.Domain.Features.Quotations;
using SalesManagement.Persistence.Features.Quotations.GetVendorQuotations.Dtos;

namespace SalesManagement.Persistence.Features.Quotations.GetVendorQuotations
{
    public class QuotationRepository : IQuotationRepository
    {
        private readonly ISepidarConnectionFactory _connectionFactory;

        public QuotationRepository(ISepidarConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        private const string Sql = """
            DECLARE @Fy INT = (SELECT MAX(fy.FiscalYearId) FROM FMK.FiscalYear AS fy);

            DECLARE @VendorCode NVARCHAR(50) = (
                SELECT dl.Code
                FROM GNR.Party AS me
                    INNER JOIN ACC.DL AS dl ON dl.DLId = me.DLRef
                WHERE me.PartyId = @PartyId
            );

            SELECT
                q.QuotationId, q.Number, q.Date, q.ExpirationDate,
                q.CustomerPartyRef, q.CustomerRealName AS CustomerName,
                q.Price, q.Discount, q.Tax, q.NetPrice, q.Closed
            FROM GNR.Grouping AS g
                INNER JOIN GNR.Party AS p ON p.CustomerGroupingRef = g.GroupingID
                INNER JOIN SLS.Quotation AS q ON q.CustomerPartyRef = p.PartyId
            WHERE g.Code = @VendorCode AND q.FiscalYearRef = @Fy
            ORDER BY q.Number DESC;

            SELECT
                qi.QuotationItemID AS QuotationItemId, qi.QuotationRef, qi.RowID AS RowId,
                qi.ItemRef, it.Code AS ItemCode, it.Title AS ItemTitle,
                qi.Quantity, qi.Fee, qi.Price, qi.Discount, qi.NetPrice, qi.Description
            FROM SLS.QuotationItem AS qi
                INNER JOIN INV.Item AS it ON it.ItemID = qi.ItemRef
                INNER JOIN SLS.Quotation AS q ON q.QuotationId = qi.QuotationRef
                INNER JOIN GNR.Party AS p ON p.PartyId = q.CustomerPartyRef
                INNER JOIN GNR.Grouping AS g ON g.GroupingID = p.CustomerGroupingRef
            WHERE g.Code = @VendorCode AND q.FiscalYearRef = @Fy;
            """;

        private const string SqlById = """
            DECLARE @VendorCode NVARCHAR(50) = (
                SELECT dl.Code
                FROM GNR.Party AS me
                    INNER JOIN ACC.DL AS dl ON dl.DLId = me.DLRef
                WHERE me.PartyId = @PartyId
            );

            SELECT
                q.QuotationId, q.Number, q.Date, q.ExpirationDate,
                q.CustomerPartyRef, q.CustomerRealName AS CustomerName,
                q.Price, q.Discount, q.Tax, q.NetPrice, q.Closed
            FROM SLS.Quotation AS q
                INNER JOIN GNR.Party AS p ON p.PartyId = q.CustomerPartyRef
                INNER JOIN GNR.Grouping AS g ON g.GroupingID = p.CustomerGroupingRef
            WHERE g.Code = @VendorCode AND q.QuotationId = @QuotationId;

            SELECT
                qi.QuotationItemID AS QuotationItemId, qi.QuotationRef, qi.RowID AS RowId,
                qi.ItemRef, it.Code AS ItemCode, it.Title AS ItemTitle,
                qi.Quantity, qi.Fee, qi.Price, qi.Discount, qi.NetPrice, qi.Description
            FROM SLS.QuotationItem AS qi
                INNER JOIN INV.Item AS it ON it.ItemID = qi.ItemRef
                INNER JOIN SLS.Quotation AS q ON q.QuotationId = qi.QuotationRef
                INNER JOIN GNR.Party AS p ON p.PartyId = q.CustomerPartyRef
                INNER JOIN GNR.Grouping AS g ON g.GroupingID = p.CustomerGroupingRef
            WHERE g.Code = @VendorCode AND q.QuotationId = @QuotationId;
            """;

        private const string SqlByCustomer = """
            DECLARE @Fy INT = (SELECT MAX(fy.FiscalYearId) FROM FMK.FiscalYear AS fy);

            DECLARE @VendorCode NVARCHAR(50) = (
                SELECT dl.Code
                FROM GNR.Party AS me
                    INNER JOIN ACC.DL AS dl ON dl.DLId = me.DLRef
                WHERE me.PartyId = @PartyId
            );

            SELECT
                q.QuotationId, q.Number, q.Date, q.ExpirationDate,
                q.CustomerPartyRef, q.CustomerRealName AS CustomerName,
                q.Price, q.Discount, q.Tax, q.NetPrice, q.Closed
            FROM GNR.Grouping AS g
                INNER JOIN GNR.Party AS p ON p.CustomerGroupingRef = g.GroupingID
                INNER JOIN SLS.Quotation AS q ON q.CustomerPartyRef = p.PartyId
            WHERE g.Code = @VendorCode
              AND q.FiscalYearRef = @Fy
              AND q.CustomerPartyRef = @CustomerPartyRef
            ORDER BY q.Number DESC;

            SELECT
                qi.QuotationItemID AS QuotationItemId, qi.QuotationRef, qi.RowID AS RowId,
                qi.ItemRef, it.Code AS ItemCode, it.Title AS ItemTitle,
                qi.Quantity, qi.Fee, qi.Price, qi.Discount, qi.NetPrice, qi.Description
            FROM SLS.QuotationItem AS qi
                INNER JOIN INV.Item AS it ON it.ItemID = qi.ItemRef
                INNER JOIN SLS.Quotation AS q ON q.QuotationId = qi.QuotationRef
                INNER JOIN GNR.Party AS p ON p.PartyId = q.CustomerPartyRef
                INNER JOIN GNR.Grouping AS g ON g.GroupingID = p.CustomerGroupingRef
            WHERE g.Code = @VendorCode
              AND q.FiscalYearRef = @Fy
              AND q.CustomerPartyRef = @CustomerPartyRef;
            """;

        public async Task<IReadOnlyList<Quotation>> GetForPartyAsync(
            int partyId, CancellationToken cancellationToken = default)
        {
            using var connection = _connectionFactory.CreateConnection();

            var command = new CommandDefinition(
                Sql, new { PartyId = partyId }, cancellationToken: cancellationToken);

            using var multi = await connection.QueryMultipleAsync(command);
            return await ReadListAsync(multi);
        }

        public async Task<Quotation?> GetForPartyByIdAsync(
            int partyId, int quotationId, CancellationToken cancellationToken = default)
        {
            using var connection = _connectionFactory.CreateConnection();

            var command = new CommandDefinition(
                SqlById, new { PartyId = partyId, QuotationId = quotationId },
                cancellationToken: cancellationToken);

            using var multi = await connection.QueryMultipleAsync(command);

            var quotationDto = await multi.ReadFirstOrDefaultAsync<QuotationDto>();
            if (quotationDto is null)
                return null;

            var itemDtos = (await multi.ReadAsync<QuotationItemDto>()).AsList();

            return Quotation.Reconstitute(
                quotationDto.QuotationId, quotationDto.Number, quotationDto.Date, quotationDto.ExpirationDate,
                quotationDto.CustomerPartyRef, quotationDto.CustomerName,
                quotationDto.Price, quotationDto.Discount, quotationDto.Tax, quotationDto.NetPrice, quotationDto.Closed,
                itemDtos.Select(ToItem).ToList());
        }

        public async Task<IReadOnlyList<Quotation>> GetForPartyByCustomerAsync(
            int partyId, int customerPartyRef, CancellationToken cancellationToken = default)
        {
            using var connection = _connectionFactory.CreateConnection();

            var command = new CommandDefinition(
                SqlByCustomer,
                new { PartyId = partyId, CustomerPartyRef = customerPartyRef },
                cancellationToken: cancellationToken);

            using var multi = await connection.QueryMultipleAsync(command);
            return await ReadListAsync(multi);
        }

        private static async Task<IReadOnlyList<Quotation>> ReadListAsync(SqlMapper.GridReader multi)
        {
            var quotationDtos = (await multi.ReadAsync<QuotationDto>()).AsList();
            var itemDtos = (await multi.ReadAsync<QuotationItemDto>()).AsList();

            var itemsByQuotation = itemDtos
                .GroupBy(x => x.QuotationRef)
                .ToDictionary(g => g.Key, g => g.Select(ToItem).ToList());

            return quotationDtos
                .Select(d =>
                {
                    itemsByQuotation.TryGetValue(d.QuotationId, out var items);
                    return Quotation.Reconstitute(
                        d.QuotationId, d.Number, d.Date, d.ExpirationDate,
                        d.CustomerPartyRef, d.CustomerName,
                        d.Price, d.Discount, d.Tax, d.NetPrice, d.Closed,
                        items ?? []);
                })
                .ToList();
        }

        private static QuotationItem ToItem(QuotationItemDto d) =>
            QuotationItem.Reconstitute(
                d.QuotationItemId, d.RowId, d.ItemRef, d.ItemCode, d.ItemTitle,
                d.Quantity, d.Fee, d.Price, d.Discount, d.NetPrice, d.Description);
    }
}