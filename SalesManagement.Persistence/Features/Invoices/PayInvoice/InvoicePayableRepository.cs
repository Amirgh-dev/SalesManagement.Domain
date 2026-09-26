using Dapper;
using SalesManagement.Domain.Features.Invoices;
using SalesManagement.Persistence.ConnectionFactories;

namespace SalesManagement.Persistence.Features.Invoices.PayInvoice
{
    public class InvoicePayableRepository(ISepidarConnectionFactory connectionFactory) : IInvoicePayableRepository
    {
        private const string SqlSelect = """
            SELECT InvoiceId, Number, NetPrice
            FROM SLS.Invoice
            WHERE InvoiceId = @InvoiceId AND CustomerPartyRef = @PartyRef;
            """;

        public async Task<PayableInvoice?> GetAsync(
            int invoiceId, int partyRef, CancellationToken cancellationToken = default)
        {
            using var connection = connectionFactory.CreateConnection();
            var row = await connection.QuerySingleOrDefaultAsync<InvoiceRow>(
                new CommandDefinition(
                    SqlSelect, new { InvoiceId = invoiceId, PartyRef = partyRef }, cancellationToken: cancellationToken));

            return row is null
                ? null
                : new PayableInvoice(
                    row.InvoiceId, row.Number, (long)Math.Round(row.NetPrice, MidpointRounding.AwayFromZero));
        }

        private sealed class InvoiceRow
        {
            public int InvoiceId { get; set; }
            public int Number { get; set; }
            public decimal NetPrice { get; set; }
        }
    }
}
