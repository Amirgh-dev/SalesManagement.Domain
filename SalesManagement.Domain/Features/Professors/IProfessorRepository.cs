namespace SalesManagement.Domain.Features.Professors
{
    public interface IProfessorRepository
    {
        Task<Guid> SaveAsync(NewProfessor professor, CancellationToken cancellationToken = default);
    }
}
