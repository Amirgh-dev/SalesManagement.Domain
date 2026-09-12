using System.ComponentModel.DataAnnotations;

namespace SalesManagement.API.Features.PartyProfiles
{
    public class SavePartyProfileRequest
    {
        [Required][MaxLength(50)] public string UserName { get; init; } = "";
        [MaxLength(500)] public string? AboutMe { get; init; }
        [MaxLength(500)] public string? Address { get; init; }
        public string? Geolocation { get; init; }
        public IFormFile? ProfilePhoto { get; init; }
        public List<IFormFile>? Photos { get; init; }
    }
}
