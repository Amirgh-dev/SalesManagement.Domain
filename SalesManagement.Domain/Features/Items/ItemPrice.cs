namespace SalesManagement.Domain.Features.Items
{
    public sealed record ItemPrice(
        int PriceNoteItemId,
        int SaleTypeRef,
        int UnitRef,
        decimal Fee,
        decimal Discount);
}
