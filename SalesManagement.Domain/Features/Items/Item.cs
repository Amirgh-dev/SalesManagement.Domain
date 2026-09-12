
namespace SalesManagement.Domain.Features.Items
{
    public class Item
    {
        private readonly List<ItemStockSummary> _stockSummaries = new();
        private readonly List<ItemPrice> _prices = new();

        public int Id { get; private set; }
        public int Type { get; private set; }
        public string Code { get; private set; } = string.Empty;
        public string Title { get; private set; } = string.Empty;
        public int UnitRef { get; private set; }
        public int? SecondaryUnitRef { get; private set; }
        public decimal UnitsRatio { get; private set; }
        public bool Sellable { get; private set; }
        public bool IsActive { get; private set; }
        public ItemGrouping Grouping { get; private set; } = null!;
        public ItemProperties? Properties { get; private set; }

        public IReadOnlyList<ItemStockSummary> StockSummaries => _stockSummaries;
        public IReadOnlyList<ItemPrice> Prices => _prices;
        public decimal TotalQuantity => _stockSummaries.Sum(s => s.Quantity);
        public bool IsAvailableForSale => IsActive && Sellable && TotalQuantity > 0;

        private Item() { }

        public static Item Reconstitute(
            int id, int type, string code, string title, int unitRef, int? secondaryUnitRef,
            decimal unitsRatio, bool sellable, bool isActive, ItemGrouping grouping,
            ItemProperties? properties,
            IEnumerable<ItemStockSummary> stockSummaries, IEnumerable<ItemPrice> prices)
        {
            var item = new Item
            {
                Id = id,
                Type = type,
                Code = code,
                Title = title,
                UnitRef = unitRef,
                SecondaryUnitRef = secondaryUnitRef,
                UnitsRatio = unitsRatio,
                Sellable = sellable,
                IsActive = isActive,
                Grouping = grouping,
                Properties = properties
            };

            item._stockSummaries.AddRange(stockSummaries);
            item._prices.AddRange(prices);
            return item;
        }
    }
}
