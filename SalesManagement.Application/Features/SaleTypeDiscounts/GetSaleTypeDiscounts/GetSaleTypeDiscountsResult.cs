namespace SalesManagement.Application.Features.SaleTypeDiscounts.GetSaleTypeDiscounts
{
    public record GetSaleTypeDiscountsResult(IReadOnlyList<SaleTypeDiscountListItem> Items);

    public record SaleTypeDiscountListItem(
        int Id,
        int SaleTypeRef,
        string? Title,
        decimal? Discount,
        int? IsCash,
        bool? BlackList);
}
