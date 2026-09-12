using Microsoft.Extensions.Logging;
using SalesManagement.Domain.Common.Exceptions;
using SalesManagement.Domain.Features.Invoices;

namespace SalesManagement.Application.Features.Invoices.QueryInvoices
{
    public class QueryInvoicesHandler(
        IInvoiceQueryRepository invoiceQueryRepository,
        ILogger<QueryInvoicesHandler> logger)
    {
        public async Task<QueryInvoicesResult> Handle(
            QueryInvoicesCommand cmd, CancellationToken cancellationToken = default)
        {
            if (cmd.PartyId <= 0)
                throw new DomainException("شناسه‌ی کاربر نامعتبر است");

            var query = InvoiceQuery.Create(
                cmd.Scope,
                cmd.Fields,
                cmd.Filters?.Select(f => (f.Field, f.Op, f.Value)),
                (cmd.Search?.Term, cmd.Search?.Fields),
                cmd.OrderBy,
                cmd.Descending,
                cmd.Skip,
                cmd.Take,
                cmd.Page,
                cmd.PageSize);

            var (total, invoices, items) = await invoiceQueryRepository.QueryAsync(
                cmd.PartyId, query, cancellationToken);

            logger.LogInformation(
                "کوئری فاکتور کاربر {PartyId}: scope={Scope}، فاکتور={InvoiceCount}، آیتم={ItemCount}",
                cmd.PartyId, query.Scope,
                invoices?.Count ?? 0, items?.Count ?? 0);

            return new QueryInvoicesResult(total, invoices, items);
        }
    }
}
