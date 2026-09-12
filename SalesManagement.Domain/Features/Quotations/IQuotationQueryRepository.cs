namespace SalesManagement.Domain.Features.Quotations
{
    public interface IQuotationQueryRepository
    {
        Task<(int Total, IReadOnlyList<IDictionary<string, object?>>? Quotations, IReadOnlyList<IDictionary<string, object?>>? Items)> QueryAsync(
            int partyId, QuotationQuery query, CancellationToken cancellationToken = default);
    }
}
