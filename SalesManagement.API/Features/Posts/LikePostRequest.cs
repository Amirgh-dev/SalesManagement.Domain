using System.ComponentModel.DataAnnotations;

namespace SalesManagement.API.Features.Posts
{
    public class LikePostRequest
    {
        [Required] public Guid? PostId { get; init; }
    }
}
