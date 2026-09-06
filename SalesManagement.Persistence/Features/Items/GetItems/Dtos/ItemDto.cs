namespace SalesManagement.Persistence.Features.Items.GetItems.Dtos
{
    /// <summary>ردیف خام کوئری کالا (ستون‌های INV.Item + GNR.Grouping + INV.ItemPropertyAmount).</summary>
    public class ItemDto
    {
        public int ItemId { get; set; }
        public int Type { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int UnitRef { get; set; }
        public int? SecondaryUnitRef { get; set; }
        public decimal UnitsRatio { get; set; }
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
    }
}
