using Microsoft.Extensions.Logging;
using SalesManagement.Application.Common.Interfaces;
using SalesManagement.Domain.Common.Exceptions;
using SalesManagement.Domain.Features.Items;

namespace SalesManagement.Application.Features.Items.QueryItems
{
    public class QueryItemsHandler(
        IItemQueryRepository itemQueryRepository,
        ISepidarConfigurationProvider configurationProvider,
        ILogger<QueryItemsHandler> logger)
    {
        public async Task<QueryItemsResult> Handle(
            QueryItemsCommand cmd, CancellationToken cancellationToken = default)
        {
            var query = ItemQuery.Create(
                cmd.Id, cmd.CategoryCode, cmd.Scope, cmd.Fields,
                cmd.SearchTerm, cmd.SearchFields, cmd.OrderBy, cmd.Descending,
                cmd.Skip, cmd.Take, cmd.Page, cmd.PageSize);

            var saleTypeRef = configurationProvider.ResolveSaleTypeRef();

            var entityType = configurationProvider.Get().ItemSaleGroupEntityType;
            if (string.IsNullOrWhiteSpace(entityType))
                throw new DomainException("ItemSaleGroupEntityType در پیکربندی سپیدار تعیین نشده است");

            var (totalItems, totalCategories, items, categories) = await itemQueryRepository.QueryAsync(
                query, saleTypeRef, entityType, cancellationToken);

            logger.LogInformation(
                "کوئری کالاها اجرا شد: TotalItems={TotalItems}, TotalCategories={TotalCategories}, Items={ItemCount}, Categories={CategoryCount}",
                totalItems, totalCategories, items?.Count ?? 0, categories?.Count ?? 0);

            return new QueryItemsResult(totalItems, totalCategories, items, categories);
        }
    }
}
