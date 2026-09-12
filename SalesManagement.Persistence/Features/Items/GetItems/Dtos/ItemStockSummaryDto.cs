namespace SalesManagement.Persistence.Features.Items.GetItems.Dtos
{
    public class ItemStockSummaryDto
    {
        public int ItemStockSummaryId { get; set; }
        public int ItemRef { get; set; }
        public int StockRef { get; set; }
        public string? StockTitle { get; set; }
        public int UnitRef { get; set; }
        public decimal InputQuantity { get; set; }
        public decimal OutputQuantity { get; set; }
        public decimal Quantity { get; set; }
        public decimal SaleQuantity { get; set; }
        public int FiscalYearRef { get; set; }
    }
}
