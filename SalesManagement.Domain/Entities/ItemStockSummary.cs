namespace SalesManagement.Domain.Entities
{
    public class ItemStockSummary
    {
        public int ItemStockSummaryId { get; private set; }
        public int StockRef { get; private set; }
        public string? StockTitle { get; private set; }
        public int UnitRef { get; private set; }
        public decimal InputQuantity { get; private set; }
        public decimal OutputQuantity { get; private set; }
        public decimal Quantity { get; private set; }
        public decimal SaleQuantity { get; private set; }
        public int FiscalYearRef { get; private set; }

        private ItemStockSummary() { }

        public static ItemStockSummary Reconstitute(
            int itemStockSummaryId, int stockRef, string? stockTitle, int unitRef,
            decimal inputQuantity, decimal outputQuantity, decimal quantity,
            decimal saleQuantity, int fiscalYearRef)
        {
            return new ItemStockSummary
            {
                ItemStockSummaryId = itemStockSummaryId,
                StockRef = stockRef,
                StockTitle = stockTitle,
                UnitRef = unitRef,
                InputQuantity = inputQuantity,
                OutputQuantity = outputQuantity,
                Quantity = quantity,
                SaleQuantity = saleQuantity,
                FiscalYearRef = fiscalYearRef
            };
        }
    }
}
