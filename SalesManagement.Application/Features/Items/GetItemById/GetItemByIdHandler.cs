using Microsoft.Extensions.Logging;
using SalesManagement.Application.Common.Interfaces;
using SalesManagement.Application.Features.Items.GetItems;
using SalesManagement.Domain.Common.Exceptions;
using SalesManagement.Domain.Features.Items;

namespace SalesManagement.Application.Features.Items.GetItemById
{
    public class GetItemByIdHandler
    {
        private readonly IItemRepository _itemRepository;
        private readonly ISepidarConfigurationProvider _configurationProvider;
        private readonly ILogger<GetItemByIdHandler> _logger;

        public GetItemByIdHandler(
            IItemRepository itemRepository,
            ISepidarConfigurationProvider configurationProvider,
            ILogger<GetItemByIdHandler> logger)
        {
            _itemRepository = itemRepository;
            _configurationProvider = configurationProvider;
            _logger = logger;
        }

        public async Task<ItemListItem> Handle(
            GetItemByIdQuery query, CancellationToken cancellationToken = default)
        {
            if (query.ItemId <= 0)
                throw new DomainException("شناسه‌ی کالا نامعتبر است");

            var saleTypeRef = _configurationProvider.Get().DefaultSaleTypeRef;
            if (saleTypeRef <= 0)
                saleTypeRef = 1;

            var item = await _itemRepository.GetByIdAsync(query.ItemId, saleTypeRef, cancellationToken);

            if (item is null)
            {
                _logger.LogWarning("کالا پیدا نشد: {ItemId}", query.ItemId);
                throw new NotFoundException($"کالایی با شناسه‌ی {query.ItemId} پیدا نشد");
            }

            return ItemResultMapper.ToListItem(item);
        }
    }
}
