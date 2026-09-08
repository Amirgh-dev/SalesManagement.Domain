namespace SalesManagement.Persistence.Features.Locations.GetLocations.Dtos
{
    public class LocationDto
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? TitleEn { get; set; }
        public string? Code { get; set; }
        public int? Parent { get; set; }
        public int Type { get; set; }
    }
}