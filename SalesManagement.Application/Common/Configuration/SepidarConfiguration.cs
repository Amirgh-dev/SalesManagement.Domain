namespace SalesManagement.Application.Common.Configuration
{
    public class SepidarConfiguration
    {
        public int DefaultFiscalYearsRef { get; set; }
        public int DefaultStockRef { get; set; }
        public int DefaultOpenBoxStockRef { get; set; }
        public int DefaultSaleTypeRef { get; set; }
        public int DefaultSaleTypeRefOpenBox { get; set; }
        public int DefaultWarrantyTypeRef { get; set; }
        public int DefaultReturnTypeRef { get; set; }
        public int DefaultUnitRef { get; set; }
        public int? DefaultSLRef { get; set; }
        public int DefaultPriceNoteItemRef { get; set; }
        public int DefaultCurrencyRef { get; set; }
        public int DefaultQuantity { get; set; }
        public int DefaultUserRef { get; set; }
        public int ItemTypes { get; set; }
        public int ItemSellable { get; set; }
        public int ItemIsActive { get; set; }
        public int ItemIsAvailable { get; set; }
        public int ItemTake { get; set; }
        public int ItemStockSummaryOrder { get; set; }
        public int UsedMultiPriceNoteItem { get; set; }
        public int UsedMultiStockItem { get; set; }
        public int UsedTypeGrouping { get; set; }
        public string? UsedEntitesGrouping { get; set; }
        public int PartyType { get; set; }
        public int PartyPhoneType { get; set; }
        public int PartyAddressType { get; set; }
        public string? ImageFormat { get; set; }
        public string? StaticServerUrl { get; set; }
        public string? SepidarVersion { get; set; }
        public int PartyPhoneIsMain { get; set; }
        public int PartyAddressIsMain { get; set; }
        public int UsedGroupingRef { get; set; }
        public int AccDLRef { get; set; }
        public int SLDLRef { get; set; }
        public int CashDLRef { get; set; }
        public int DefaultCustomerRef { get; set; }
        public int DefaultDLRef { get; set; }
        public int DefaultBankRef { get; set; }
        public int DefaultPosRef { get; set; }
        public int DatabaseServer { get; set; }
        public string? DatabaseName { get; set; }
        public string? DatabaseUsername { get; set; }
        public string? DatabasePassword { get; set; }
        public string? DatabasePort { get; set; }
        public int? DefaultSaleTypeRefWarranty { get; set; }
        public string? ItemSaleGroupEntityType { get; set; }
        public int DefaultLocationRef { get; set; }
        public int LocationProvinceType { get; set; }
        public int LocationCountyType { get; set; }
    }
}
