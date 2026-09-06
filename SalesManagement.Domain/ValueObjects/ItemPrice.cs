namespace SalesManagement.Domain.ValueObjects
{
    /// <summary>یک ردیف قیمت فروش کالا برای یک نوع فروش مشخص.</summary>
    public sealed record ItemPrice(
        int PriceNoteItemId,
        int SaleTypeRef,
        int UnitRef,
        decimal Fee,
        decimal Discount);
}
