using Microsoft.Extensions.Logging;
using SalesManagement.Domain.Features.SaleTypeDiscounts;

namespace SalesManagement.Application.Features.SaleTypeDiscounts.GetSaleTypeDiscounts
{
    public class GetSaleTypeDiscountsHandler(
        ISaleTypeDiscountRepository repository,
        ILogger<GetSaleTypeDiscountsHandler> logger)
    {
        public async Task<GetSaleTypeDiscountsResult> Handle(CancellationToken cancellationToken = default)
        {
            var rows = await repository.GetAllAsync(cancellationToken);

            logger.LogInformation("فهرست تخفیف نوع فروش خوانده شد: {Count}", rows.Count);

            return new GetSaleTypeDiscountsResult(
                rows
                    .Select(r => new SaleTypeDiscountListItem(
                        r.Id, r.SaleTypeRef, r.Title, r.Discount, r.IsCash, r.BlackList))
                    .ToList());
        }
    }
}
