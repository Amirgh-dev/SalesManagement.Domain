using System.ComponentModel.DataAnnotations;

namespace SalesManagement.API.Features.PartyProfiles
{
    public class SaveProfilePhotoRequest
    {
        [Required] public IFormFile? Photo { get; init; }
    }
}
