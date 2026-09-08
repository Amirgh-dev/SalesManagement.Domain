namespace SalesManagement.Application.Features.Locations.GetLocations
{
    public record GetLocationsResult(IReadOnlyList<LocationListItem> Locations);

    public record LocationListItem(
        int Id,
        string? Title,
        string? TitleEn,
        string? Code,
        int? Parent,
        int Type);
}