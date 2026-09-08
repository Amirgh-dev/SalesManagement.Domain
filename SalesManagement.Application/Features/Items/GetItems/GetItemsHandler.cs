using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using SalesManagement.Application.Common.Interfaces;
using SalesManagement.Domain.Features.Items;

namespace SalesManagement.Application.Features.Items.GetItems
{
    public class GetItemsHandler
    {
        private readonly IItemRepository _itemRepository;
        private readonly ISepidarConfigurationProvider _configurationProvider;
        private readonly HybridCache _cache;
        private readonly ILogger<GetItemsHandler> _logger;

        public GetItemsHandler(
            IItemRepository itemRepository,
            ISepidarConfigurationProvider configurationProvider,
            HybridCache cache,
            ILogger<GetItemsHandler> logger)
        {
            _itemRepository = itemRepository;
            _configurationProvider = configurationProvider;
            _cache = cache;
            _logger = logger;
        }

        public async Task<GetItemsResult> Handle(
            GetItemsQuery query, CancellationToken cancellationToken = default)
        {
            var saleTypeRef = _configurationProvider.ResolveSaleTypeRef();

            return await _cache.GetOrCreateAsync(
                $"items:catalog:{saleTypeRef}",
                async ct =>
                {
                    var items = await _itemRepository.GetCatalogAsync(saleTypeRef, ct);

                    _logger.LogInformation(
                        "فهرست کالاها از دیتابیس خوانده شد: {Count} کالا (SaleTypeRef={SaleTypeRef})",
                        items.Count, saleTypeRef);

                    return new GetItemsResult(items.Select(ItemResultMapper.ToListItem).ToList());
                },
                tags: ["items"],
                cancellationToken: cancellationToken);
        }
    }
}
