using SalesManagement.Domain.Entities;

namespace SalesManagement.Domain.Features.Items
{
    public interface IItemRepository
    {
        /// <summary>فهرست سبک همه‌ی کالاها (گروه + ویژگی + مجموع موجودی + قیمت واحد اصلی).</summary>
        Task<IReadOnlyList<CatalogItem>> GetCatalogAsync(
            int saleTypeRef, CancellationToken cancellationToken = default);

        /// <summary>فهرست سبکِ کالاهای فعالِ یک دسته بر اساس کدِ گروه فروش.</summary>
        Task<IReadOnlyList<CatalogItem>> GetCatalogByCategoryAsync(
            string categoryCode, int saleTypeRef, CancellationToken cancellationToken = default);

        /// <summary>جزئیات کاملِ یک کالا بر اساس شناسه (شامل آرایه‌ی موجودی و قیمت)؛ اگر نبود <c>null</c>.</summary>
        Task<Item?> GetByIdAsync(
            int itemId, int saleTypeRef, CancellationToken cancellationToken = default);
    }
}
