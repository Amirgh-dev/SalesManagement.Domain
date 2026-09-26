using System.ComponentModel.DataAnnotations;

namespace SalesManagement.API.Features.Files
{
    public class DeleteFileRequest
    {
        [Required] public Guid? Id { get; init; }
    }
}
