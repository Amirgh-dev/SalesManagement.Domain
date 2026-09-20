namespace SalesManagement.Domain.Features.Courses
{
    public interface ICourseRepository
    {
        Task<(Guid Id, IReadOnlyList<string> RemovedFileNames)> SaveAsync(
            NewCourse course, CancellationToken cancellationToken = default);
    }
}
