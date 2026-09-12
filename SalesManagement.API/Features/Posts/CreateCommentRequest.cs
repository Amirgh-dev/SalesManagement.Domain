using System.ComponentModel.DataAnnotations;

namespace SalesManagement.API.Features.Posts
{
    public class CreateCommentRequest
    {
        public Guid? Id { get; init; }
        [Required] public Guid? PostRef { get; init; }
        [Required] public Guid? ProfileRef { get; init; }
        [Required][MaxLength(500)] public string Comment { get; init; } = "";
    }
}
