namespace SalesManagement.Application.Features.Professors.CreateProfessor
{
    public record CreateProfessorCommand(Guid? Id, string? Name, string? LastName, string? Specialization, bool IsOk);
}
