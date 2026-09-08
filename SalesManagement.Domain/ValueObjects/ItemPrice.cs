namespace SalesManagement.Domain.ValueObjects
{
    public sealed record ItemPrice(
        int PriceNoteItemId,
        int SaleTypeRef,
        int UnitRef,
        decimal Fee,
        decimal Discount);
}
