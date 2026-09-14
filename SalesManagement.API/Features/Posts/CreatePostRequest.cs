using System.ComponentModel.DataAnnotations;
using SalesManagement.Domain.Enums;

namespace SalesManagement.API.Features.Posts
{
    public class CreatePostRequest
    {
        public Guid? Id { get; init; }
        [Required] public Guid? ProfileRef { get; init; }
        [Required][MaxLength(50)] public string Title { get; init; } = "";
        [Required][MaxLength(500)] public string Post { get; init; } = "";
        public TypePost? Type { get; init; }
        public List<IFormFile>? Images { get; init; }
    }
}
