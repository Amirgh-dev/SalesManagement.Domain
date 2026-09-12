using System.ComponentModel.DataAnnotations;

namespace SalesManagement.API.Features.Posts
{
    public class CreatePostRequest
    {
        public Guid? Id { get; init; }
        [Required] public Guid? ProfileRef { get; init; }
        [Required][MaxLength(50)] public string Title { get; init; } = "";
        [Required][MaxLength(500)] public string Post { get; init; } = "";
        public List<IFormFile>? Images { get; init; }
        public List<Guid>? RemovePhotoIds { get; init; }
    }
}
