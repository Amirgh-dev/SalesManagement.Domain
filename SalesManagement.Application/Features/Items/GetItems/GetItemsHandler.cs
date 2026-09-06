using Microsoft.Extensions.Logging;
using SalesManagement.Application.Common.Interfaces;

namespace SalesManagement.Application.Features.Items.GetItems
{
    public class GetItemsHandler
    {
        private readonly IItemReadRepository _itemRepository;
        private readonly ISepidarConfigurationProvider _configurationProvider;
        private readonly ILogger<GetItemsHandler> _logger;

        public GetItemsHandler(
            IItemReadRepository itemRepository,
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

            var items = await _itemRepository.GetItemsAsync(saleTypeRef, cancellationToken);

            _logger.LogInformation(
                "فهرست کالاها خوانده شد: {Count} کالا (SaleTypeRef={SaleTypeRef})",
                items.Count, saleTypeRef);

            return new GetItemsResult(items);
        }
    }
}
