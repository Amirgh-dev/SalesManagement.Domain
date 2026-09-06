namespace SalesManagement.Application.Features.Items.GetItems
{
    public interface IItemReadRepository
    {
        /// <summary>
        /// فهرست کالاها به‌همراه گروه‌بندی، خلاصه‌ی موجودی انبارهای فعال و قیمت فروش
        /// برای نوع فروش مشخص‌شده را برمی‌گرداند.
        /// </summary>
        Task<IReadOnlyList<ItemListItem>> GetItemsAsync(
            int saleTypeRef, CancellationToken cancellationToken = default);
    }
}
