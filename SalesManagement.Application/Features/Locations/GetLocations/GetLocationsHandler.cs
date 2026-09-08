using Microsoft.Extensions.Logging;
using SalesManagement.Application.Common.Interfaces;
using SalesManagement.Domain.Entities;
using SalesManagement.Domain.Features.Locations;

namespace SalesManagement.Application.Features.Locations.GetLocations
{
    public class GetLocationsHandler(
        ILocationRepository locationRepository,
        ISepidarConfigurationProvider configurationProvider,
        ILogger<GetLocationsHandler> logger)
    {
        public async Task<GetLocationsResult> GetProvincesAsync(CancellationToken cancellationToken = default)
        {
            var type = configurationProvider.Get().LocationProvinceType;
            var locations = await locationRepository.GetByTypeAsync(type, cancellationToken);

            logger.LogInformation("فهرست استان‌ها خوانده شد: {Count}", locations.Count);

            return Map(locations);
        }

        public async Task<GetLocationsResult> GetCountiesAsync(
            int provinceId, CancellationToken cancellationToken = default)
        {
            var type = configurationProvider.Get().LocationCountyType;
            var locations = await locationRepository.GetAsync(type, provinceId, cancellationToken);

            logger.LogInformation(
                "فهرست شهرستان‌های استان {ProvinceId} خوانده شد: {Count}", provinceId, locations.Count);

            return Map(locations);
        }

        private static GetLocationsResult Map(IReadOnlyList<Location> locations) =>
            new(locations
                .Select(l => new LocationListItem(l.Id, l.Title, l.TitleEn, l.Code, l.Parent, l.Type))
                .ToList());
    }
}
