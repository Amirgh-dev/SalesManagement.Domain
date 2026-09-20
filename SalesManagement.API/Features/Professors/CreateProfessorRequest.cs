using System.ComponentModel.DataAnnotations;

namespace SalesManagement.API.Features.Professors
{
    public class CreateProfessorRequest
    {
        public Guid? Id { get; init; }
        [Required][MaxLength(50)] public string Name { get; init; } = "";
        [Required][MaxLength(50)] public string LastName { get; init; } = "";
        [MaxLength(500)] public string? Specialization { get; init; }
        public bool IsOk { get; init; }
    }
}
