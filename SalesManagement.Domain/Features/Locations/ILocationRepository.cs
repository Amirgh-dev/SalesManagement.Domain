namespace SalesManagement.Domain.Features.Locations
{
    public interface ILocationRepository
    {
        Task<IReadOnlyList<Location>> GetByTypeAsync(
            int type, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Location>> GetAsync(
            int type, int parent, CancellationToken cancellationToken = default);
    }
}