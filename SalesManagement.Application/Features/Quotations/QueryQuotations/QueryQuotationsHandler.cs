using Microsoft.Extensions.Logging;
using SalesManagement.Domain.Common.Exceptions;
using SalesManagement.Domain.Features.Quotations;

namespace SalesManagement.Application.Features.Quotations.QueryQuotations
{
    public class QueryQuotationsHandler(
        IQuotationQueryRepository quotationQueryRepository,
        ILogger<QueryQuotationsHandler> logger)
    {
        public async Task<QueryQuotationsResult> Handle(
            QueryQuotationsCommand cmd, CancellationToken cancellationToken = default)
        {
            if (cmd.PartyId <= 0)
                throw new DomainException("شناسه‌ی کاربر نامعتبر است");

            var query = QuotationQuery.Create(
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

            var (total, quotations, items) = await quotationQueryRepository.QueryAsync(
                cmd.PartyId, query, cancellationToken);

            logger.LogInformation(
                "کوئری پیش‌فاکتور کاربر {PartyId}: scope={Scope}، پیش‌فاکتور={QuotationCount}، آیتم={ItemCount}",
                cmd.PartyId, query.Scope,
                quotations?.Count ?? 0, items?.Count ?? 0);

            return new QueryQuotationsResult(total, quotations, items);
        }
    }
}
