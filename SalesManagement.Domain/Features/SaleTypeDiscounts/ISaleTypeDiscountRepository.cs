namespace SalesManagement.Domain.Features.SaleTypeDiscounts
{
    public interface ISaleTypeDiscountRepository
    {
        Task<IReadOnlyList<SaleTypeDiscount>> GetAllAsync(CancellationToken cancellationToken = default);
    }
}
