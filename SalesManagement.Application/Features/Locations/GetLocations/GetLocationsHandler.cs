using Microsoft.Extensions.Logging;
using SalesManagement.Application.Common.Interfaces;
using SalesManagement.Domain.Features.Locations;

namespace SalesManagement.Application.Features.Locations.GetLocations
{
    public class GetLocationsHandler(
        ILocationRepository locationRepository,
        ISepidarConfigurationProvider configurationProvider,
        ILogger<GetLocationsHandler> logger)
    {
        public async Task<GetLocationsResult> Handle(
            GetLocationsQuery query, CancellationToken cancellationToken = default)
        {
            var config = configurationProvider.Get();

            if (query.ProvinceId is { } provinceId)
            {
                var counties = await locationRepository.GetAsync(
                    config.LocationCountyType, provinceId, cancellationToken);

                logger.LogInformation(
                    "فهرست شهرستان‌های استان {ProvinceId} خوانده شد: {Count}", provinceId, counties.Count);

                return Map(counties);
            }

            var provinces = await locationRepository.GetByTypeAsync(
                config.LocationProvinceType, cancellationToken);

            logger.LogInformation("فهرست استان‌ها خوانده شد: {Count}", provinces.Count);

            return Map(provinces);
        }

        private static GetLocationsResult Map(IReadOnlyList<Location> locations) =>
            new(locations
                .Select(l => new LocationListItem(l.Id, l.Title, l.TitleEn, l.Code, l.Parent, l.Type))
                .ToList());
    }
}
