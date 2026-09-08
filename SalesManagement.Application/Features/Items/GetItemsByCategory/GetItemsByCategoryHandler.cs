using Microsoft.Extensions.Logging;
using SalesManagement.Application.Common.Interfaces;
using SalesManagement.Application.Features.Items.GetItems;
using SalesManagement.Domain.Common.Exceptions;
using SalesManagement.Domain.Features.Items;

namespace SalesManagement.Application.Features.Items.GetItemsByCategory
{
    public class GetItemsByCategoryHandler
    {
        private readonly IItemRepository _itemRepository;
        private readonly ISepidarConfigurationProvider _configurationProvider;
        private readonly ILogger<GetItemsByCategoryHandler> _logger;

        public GetItemsByCategoryHandler(
            IItemRepository itemRepository,
            ISepidarConfigurationProvider configurationProvider,
            ILogger<GetItemsByCategoryHandler> logger)
        {
            _itemRepository = itemRepository;
            _configurationProvider = configurationProvider;
            _logger = logger;
        }

        public async Task<GetItemsResult> Handle(
            GetItemsByCategoryQuery query, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(query.CategoryCode))
                throw new DomainException("کدِ دسته الزامی است");

            var saleTypeRef = _configurationProvider.ResolveSaleTypeRef();

            var items = await _itemRepository.GetCatalogByCategoryAsync(
                query.CategoryCode.Trim(), saleTypeRef, cancellationToken);

            _logger.LogInformation(
                "کالاهای دسته {CategoryCode} خوانده شد: {Count} کالا", query.CategoryCode, items.Count);

            return new GetItemsResult(items.Select(ItemResultMapper.ToListItem).ToList());
        }
    }
}
