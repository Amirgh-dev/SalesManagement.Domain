
namespace SalesManagement.Domain.Features.Items
{
    public class CatalogItem
    {
        public int Id { get; private set; }
        public string Code { get; private set; } = string.Empty;
        public string Title { get; private set; } = string.Empty;
        public int UnitRef { get; private set; }
        public bool Sellable { get; private set; }
        public bool IsActive { get; private set; }
        public ItemGrouping Grouping { get; private set; } = null!;
        public ItemProperties? Properties { get; private set; }
        public decimal TotalQuantity { get; private set; }
        public decimal? MainPrice { get; private set; }
        public bool IsAvailableForSale => IsActive && Sellable && TotalQuantity > 0;
        private CatalogItem() { }

        public static CatalogItem Reconstitute(
            int id, string code, string title, int unitRef, bool sellable, bool isActive,
            ItemGrouping grouping, ItemProperties? properties,
            decimal totalQuantity, decimal? mainPrice)
        {
            return new CatalogItem
            {
                Id = id,
                Code = code,
                Title = title,
                UnitRef = unitRef,
                Sellable = sellable,
                IsActive = isActive,
                Grouping = grouping,
                Properties = properties,
                TotalQuantity = totalQuantity,
                MainPrice = mainPrice
            };
        }
    }
}
