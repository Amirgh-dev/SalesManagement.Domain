namespace SalesManagement.Persistence.Features.Items.GetItems.Dtos
{
    /// <summary>ردیف خامِ کوئری تختِ کاتالوگ (یک ردیف به‌ازای هر کالا).</summary>
    public class CatalogItemDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int UnitRef { get; set; }
        public bool Sellable { get; set; }
        public bool IsActive { get; set; }
        public int GroupingId { get; set; }
        public string? GroupingCode { get; set; }
        public string? GroupingTitle { get; set; }
        public int? ParentGroupRef { get; set; }
        public int? ItemPropertyAmountId { get; set; }
        public string? BodyMaterial { get; set; }
        public string? BodyColor { get; set; }
        public string? OpticalSpectrum { get; set; }
        public string? ProductModel { get; set; }
        public string? Power { get; set; }
        public string? Coverage { get; set; }
        public string? QuantityCarton { get; set; }
        public decimal TotalQuantity { get; set; }
        public decimal? MainPrice { get; set; }
    }
}
