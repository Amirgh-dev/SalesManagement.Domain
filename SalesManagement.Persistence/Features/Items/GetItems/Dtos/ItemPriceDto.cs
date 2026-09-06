namespace SalesManagement.Persistence.Features.Items.GetItems.Dtos
{
    /// <summary>ردیف خام کوئری قیمت فروش (SLS.PriceNoteItem).</summary>
    public class ItemPriceDto
    {
        public int PriceNoteItemId { get; set; }
        public int ItemRef { get; set; }
        public int SaleTypeRef { get; set; }
        public int UnitRef { get; set; }
        public decimal Fee { get; set; }
        public decimal Discount { get; set; }
    }
}
