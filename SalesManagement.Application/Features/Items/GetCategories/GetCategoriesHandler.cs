using Microsoft.Extensions.Logging;
using SalesManagement.Application.Common.Interfaces;
using SalesManagement.Domain.Features.Items;

namespace SalesManagement.Application.Features.Items.GetCategories
{
    public class GetCategoriesHandler
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly ISepidarConfigurationProvider _configurationProvider;
        private readonly ILogger<GetCategoriesHandler> _logger;

        public GetCategoriesHandler(
            ICategoryRepository categoryRepository,
            ISepidarConfigurationProvider configurationProvider,
            ILogger<GetCategoriesHandler> logger)
        {
            _categoryRepository = categoryRepository;
            _configurationProvider = configurationProvider;
            _logger = logger;
        }

        public async Task<GetCategoriesResult> Handle(
            GetCategoriesQuery query, CancellationToken cancellationToken = default)
        {
            var entityType = _configurationProvider.Get().ItemSaleGroupEntityType;
            if (string.IsNullOrWhiteSpace(entityType))
                throw new InvalidOperationException(
                    "ItemSaleGroupEntityType در پیکربندی سپیدار تعیین نشده است");

            var categories = await _categoryRepository.GetSaleGroupsAsync(entityType, cancellationToken);

            _logger.LogInformation("فهرست دسته‌ها خوانده شد: {Count} دسته", categories.Count);

            return new GetCategoriesResult(categories
                .Select(c => new CategoryListItem(c.Id, c.Code, c.Title, c.ParentGroupRef, c.IsTopLevel))
                .ToList());
        }
    }
}
