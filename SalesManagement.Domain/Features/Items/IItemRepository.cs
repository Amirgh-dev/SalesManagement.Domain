using SalesManagement.Domain.Entities;

namespace SalesManagement.Domain.Features.Items
{
    public interface IItemRepository
    {
        /// <summary>همه‌ی کالاها به‌همراه گروه، ویژگی‌ها، موجودی و قیمت (برای نوع فروش داده‌شده).</summary>
        Task<IReadOnlyList<Item>> GetAllAsync(
            int saleTypeRef, CancellationToken cancellationToken = default);

        /// <summary>یک کالا بر اساس شناسه؛ اگر پیدا نشود <c>null</c>.</summary>
        Task<Item?> GetByIdAsync(
            int itemId, int saleTypeRef, CancellationToken cancellationToken = default);

        /// <summary>کالاهای فعالِ یک دسته بر اساس کدِ گروه فروش (GNR.Grouping.Code).</summary>
        Task<IReadOnlyList<Item>> GetByCategoryCodeAsync(
            string categoryCode, int saleTypeRef, CancellationToken cancellationToken = default);
    }
}
