namespace SalesManagement.Application.Features.Items
{
    public record ItemGroupingInfo(int GroupingId, string? Code, string? Title, int? ParentGroupRef);

    public record ItemPropertiesInfo(
        int ItemPropertyAmountId,
        string? BodyMaterial,
        string? BodyColor,
        string? OpticalSpectrum,
        string? ProductModel,
        string? Power,
        string? Coverage,
        string? QuantityCarton);
}
