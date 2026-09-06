using Microsoft.Extensions.Logging;
using SalesManagement.Application.Common.Interfaces;
using SalesManagement.Domain.Features.Items;

namespace SalesManagement.Application.Features.Items.GetItems
{
    public class GetItemsHandler
    {
        private readonly IItemRepository _itemRepository;
        private readonly ISepidarConfigurationProvider _configurationProvider;
        private readonly ILogger<GetItemsHandler> _logger;

        public GetItemsHandler(
            IItemRepository itemRepository,
            ISepidarConfigurationProvider configurationProvider,
            ILogger<GetItemsHandler> logger)
        {
            _itemRepository = itemRepository;
            _configurationProvider = configurationProvider;
            _logger = logger;
        }

        public async Task<GetItemsResult> Handle(
            GetItemsQuery query, CancellationToken cancellationToken = default)
        {
            var saleTypeRef = _configurationProvider.Get().DefaultSaleTypeRef;
            if (saleTypeRef <= 0)
                saleTypeRef = 1;

            var items = await _itemRepository.GetAllAsync(saleTypeRef, cancellationToken);

            _logger.LogInformation(
                "فهرست کالاها خوانده شد: {Count} کالا (SaleTypeRef={SaleTypeRef})",
                items.Count, saleTypeRef);

            return new GetItemsResult(items.Select(ItemResultMapper.ToListItem).ToList());
        }
    }
}
