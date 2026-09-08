namespace SalesManagement.Application.Features.Items
{
    public record ItemGroupingInfo(int GroupingId, string? Code, string? Title, int? ParentGroupRef);

    public record ItemPropertiesInfo(
        int ItemPropertyAmountId,
        string? BodyMaterial,     // PropertyAmount1  - جنس بدنه
        string? BodyColor,        // PropertyAmount2  - رنگ بدنه
        string? OpticalSpectrum,  // PropertyAmount3  - طیف نوری
        string? ProductModel,     // PropertyAmount4  - مدل محصول
        string? Power,            // PropertyAmount5  - توان
        string? Coverage,         // PropertyAmount6  - پوشش
        string? QuantityCarton);  // PropertyAmount10 - تعداد در کارتن
}
