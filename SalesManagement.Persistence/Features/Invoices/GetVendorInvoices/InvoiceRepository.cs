using Dapper;
using SalesManagement.Application.Common.Interfaces;
using SalesManagement.Domain.Entities;
using SalesManagement.Domain.Features.Invoices;
using SalesManagement.Persistence.Features.Invoices.GetVendorInvoices.Dtos;

namespace SalesManagement.Persistence.Features.Invoices.GetVendorInvoices
{
    public class InvoiceRepository : IInvoiceRepository
    {
        private readonly ISepidarConnectionFactory _connectionFactory;

        public InvoiceRepository(ISepidarConnectionFactory connectionFactory)
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
                i.InvoiceId, i.Number, i.Date,
                i.CustomerPartyRef, i.CustomerRealName AS CustomerName,
                i.Price, i.Discount, i.Tax, i.NetPrice,
                CAST(CASE WHEN i.State = 0 THEN 0 ELSE 1 END AS bit) AS Closed
            FROM GNR.Grouping AS g
                INNER JOIN GNR.Party AS p ON p.CustomerGroupingRef = g.GroupingID
                INNER JOIN SLS.Invoice AS i ON i.CustomerPartyRef = p.PartyId
            WHERE g.Code = @VendorCode AND i.FiscalYearRef = @Fy
            ORDER BY i.Number DESC;

            SELECT
                ii.InvoiceItemID AS InvoiceItemId, ii.InvoiceRef, ii.RowID AS RowId,
                ii.ItemRef, it.Code AS ItemCode, it.Title AS ItemTitle,
                ii.Quantity, ii.Fee, ii.Price, ii.Discount, ii.NetPrice, ii.Description
            FROM SLS.InvoiceItem AS ii
                INNER JOIN INV.Item AS it ON it.ItemID = ii.ItemRef
                INNER JOIN SLS.Invoice AS i ON i.InvoiceId = ii.InvoiceRef
                INNER JOIN GNR.Party AS p ON p.PartyId = i.CustomerPartyRef
                INNER JOIN GNR.Grouping AS g ON g.GroupingID = p.CustomerGroupingRef
            WHERE g.Code = @VendorCode AND i.FiscalYearRef = @Fy;
            """;

        private const string SqlById = """
            DECLARE @VendorCode NVARCHAR(50) = (
                SELECT dl.Code
                FROM GNR.Party AS me
                    INNER JOIN ACC.DL AS dl ON dl.DLId = me.DLRef
                WHERE me.PartyId = @PartyId
            );

            SELECT
                i.InvoiceId, i.Number, i.Date,
                i.CustomerPartyRef, i.CustomerRealName AS CustomerName,
                i.Price, i.Discount, i.Tax, i.NetPrice,
                CAST(CASE WHEN i.State = 0 THEN 0 ELSE 1 END AS bit) AS Closed
            FROM SLS.Invoice AS i
                INNER JOIN GNR.Party AS p ON p.PartyId = i.CustomerPartyRef
                INNER JOIN GNR.Grouping AS g ON g.GroupingID = p.CustomerGroupingRef
            WHERE g.Code = @VendorCode AND i.InvoiceId = @InvoiceId;

            SELECT
                ii.InvoiceItemID AS InvoiceItemId, ii.InvoiceRef, ii.RowID AS RowId,
                ii.ItemRef, it.Code AS ItemCode, it.Title AS ItemTitle,
                ii.Quantity, ii.Fee, ii.Price, ii.Discount, ii.NetPrice, ii.Description
            FROM SLS.InvoiceItem AS ii
                INNER JOIN INV.Item AS it ON it.ItemID = ii.ItemRef
                INNER JOIN SLS.Invoice AS i ON i.InvoiceId = ii.InvoiceRef
                INNER JOIN GNR.Party AS p ON p.PartyId = i.CustomerPartyRef
                INNER JOIN GNR.Grouping AS g ON g.GroupingID = p.CustomerGroupingRef
            WHERE g.Code = @VendorCode AND i.InvoiceId = @InvoiceId;
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
                i.InvoiceId, i.Number, i.Date,
                i.CustomerPartyRef, i.CustomerRealName AS CustomerName,
                i.Price, i.Discount, i.Tax, i.NetPrice,
                CAST(CASE WHEN i.State = 0 THEN 0 ELSE 1 END AS bit) AS Closed
            FROM GNR.Grouping AS g
                INNER JOIN GNR.Party AS p ON p.CustomerGroupingRef = g.GroupingID
                INNER JOIN SLS.Invoice AS i ON i.CustomerPartyRef = p.PartyId
            WHERE g.Code = @VendorCode
              AND i.FiscalYearRef = @Fy
              AND i.CustomerPartyRef = @CustomerPartyRef
            ORDER BY i.Number DESC;

            SELECT
                ii.InvoiceItemID AS InvoiceItemId, ii.InvoiceRef, ii.RowID AS RowId,
                ii.ItemRef, it.Code AS ItemCode, it.Title AS ItemTitle,
                ii.Quantity, ii.Fee, ii.Price, ii.Discount, ii.NetPrice, ii.Description
            FROM SLS.InvoiceItem AS ii
                INNER JOIN INV.Item AS it ON it.ItemID = ii.ItemRef
                INNER JOIN SLS.Invoice AS i ON i.InvoiceId = ii.InvoiceRef
                INNER JOIN GNR.Party AS p ON p.PartyId = i.CustomerPartyRef
                INNER JOIN GNR.Grouping AS g ON g.GroupingID = p.CustomerGroupingRef
            WHERE g.Code = @VendorCode
              AND i.FiscalYearRef = @Fy
              AND i.CustomerPartyRef = @CustomerPartyRef;
            """;

        public async Task<IReadOnlyList<Invoice>> GetForPartyAsync(
            int partyId, CancellationToken cancellationToken = default)
        {
            using var connection = _connectionFactory.CreateConnection();

            var command = new CommandDefinition(
                Sql, new { PartyId = partyId }, cancellationToken: cancellationToken);

            using var multi = await connection.QueryMultipleAsync(command);
            return await ReadListAsync(multi);
        }

        public async Task<Invoice?> GetForPartyByIdAsync(
            int partyId, int invoiceId, CancellationToken cancellationToken = default)
        {
            using var connection = _connectionFactory.CreateConnection();

            var command = new CommandDefinition(
                SqlById, new { PartyId = partyId, InvoiceId = invoiceId },
                cancellationToken: cancellationToken);

            using var multi = await connection.QueryMultipleAsync(command);

            var invoiceDto = await multi.ReadFirstOrDefaultAsync<InvoiceDto>();
            if (invoiceDto is null)
                return null;

            var itemDtos = (await multi.ReadAsync<InvoiceItemDto>()).AsList();

            return Invoice.Reconstitute(
                invoiceDto.InvoiceId, invoiceDto.Number, invoiceDto.Date, invoiceDto.ExpirationDate,
                invoiceDto.CustomerPartyRef, invoiceDto.CustomerName,
                invoiceDto.Price, invoiceDto.Discount, invoiceDto.Tax, invoiceDto.NetPrice, invoiceDto.Closed,
                itemDtos.Select(ToItem).ToList());
        }

        public async Task<IReadOnlyList<Invoice>> GetForPartyByCustomerAsync(
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

        private static async Task<IReadOnlyList<Invoice>> ReadListAsync(SqlMapper.GridReader multi)
        {
            var invoiceDtos = (await multi.ReadAsync<InvoiceDto>()).AsList();
            var itemDtos = (await multi.ReadAsync<InvoiceItemDto>()).AsList();

            var itemsByInvoice = itemDtos
                .GroupBy(x => x.InvoiceRef)
                .ToDictionary(g => g.Key, g => g.Select(ToItem).ToList());

            return invoiceDtos
                .Select(d =>
                {
                    itemsByInvoice.TryGetValue(d.InvoiceId, out var items);
                    return Invoice.Reconstitute(
                        d.InvoiceId, d.Number, d.Date, d.ExpirationDate,
                        d.CustomerPartyRef, d.CustomerName,
                        d.Price, d.Discount, d.Tax, d.NetPrice, d.Closed,
                        items ?? []);
                })
                .ToList();
        }

        private static InvoiceItem ToItem(InvoiceItemDto d) =>
            InvoiceItem.Reconstitute(
                d.InvoiceItemId, d.RowId, d.ItemRef, d.ItemCode, d.ItemTitle,
                d.Quantity, d.Fee, d.Price, d.Discount, d.NetPrice, d.Description);
    }
}
