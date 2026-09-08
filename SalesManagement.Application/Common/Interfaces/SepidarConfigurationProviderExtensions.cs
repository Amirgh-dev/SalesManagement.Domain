namespace SalesManagement.Application.Common.Interfaces
{
    public static class SepidarConfigurationProviderExtensions
    {
        public static int ResolveSaleTypeRef(this ISepidarConfigurationProvider provider)
        {
            var saleTypeRef = provider.Get().DefaultSaleTypeRef;
            return saleTypeRef > 0 ? saleTypeRef : 1;
        }
    }
}
