using System.ComponentModel.DataAnnotations;

namespace SalesManagement.API.Features.Posts
{
    public class DeletePostRequest
    {
        [Required] public Guid? Id { get; init; }
        [Required] public Guid? ProfileRef { get; init; }
    }
}
