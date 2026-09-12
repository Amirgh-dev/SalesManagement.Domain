namespace SalesManagement.Domain.Features.Items
{
    public sealed record ItemProperties(
        int ItemPropertyAmountId,
        string? BodyMaterial,
        string? BodyColor,
        string? OpticalSpectrum,
        string? ProductModel,
        string? Power,
        string? Coverage,
        string? QuantityCarton);
}
