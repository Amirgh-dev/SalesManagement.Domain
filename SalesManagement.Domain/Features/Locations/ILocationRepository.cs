using SalesManagement.Domain.Entities;

namespace SalesManagement.Domain.Features.Locations
{
    public interface ILocationRepository
    {
        /// <summary>یک سطحِ محل بر اساس نوع (بدون فیلترِ والد) — مثلِ همه‌ی استان‌ها.</summary>
        Task<IReadOnlyList<Location>> GetByTypeAsync(
            int type, CancellationToken cancellationToken = default);

        /// <summary>یک سطحِ محل بر اساس نوع و والد — مثلِ شهرستان‌های یک استان.</summary>
        Task<IReadOnlyList<Location>> GetAsync(
            int type, int parent, CancellationToken cancellationToken = default);
    }
}