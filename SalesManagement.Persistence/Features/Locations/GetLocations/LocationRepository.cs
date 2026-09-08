using Dapper;
using SalesManagement.Application.Common.Interfaces;
using SalesManagement.Domain.Entities;
using SalesManagement.Domain.Features.Locations;
using SalesManagement.Persistence.Features.Locations.GetLocations.Dtos;

namespace SalesManagement.Persistence.Features.Locations.GetLocations
{
    public class LocationRepository(ISepidarConnectionFactory connectionFactory) : ILocationRepository
    {
        private const string SqlByType = """
                                         SELECT
                                             l.LocationId AS Id,
                                             l.Title,
                                             l.Title_En AS TitleEn,
                                             l.Code,
                                             l.Parent,
                                             l.Type
                                         FROM GNR.Location AS l
                                         WHERE l.Type = @Type
                                         ORDER BY l.Title;
                                         """;

        private const string SqlByTypeAndParent = """
                                                  SELECT
                                                      l.LocationId AS Id,
                                                      l.Title,
                                                      l.Title_En AS TitleEn,
                                                      l.Code,
                                                      l.Parent,
                                                      l.Type
                                                  FROM GNR.Location AS l
                                                  WHERE l.Type = @Type AND l.Parent = @Parent
                                                  ORDER BY l.Title;
                                                  """;

        public async Task<IReadOnlyList<Location>> GetByTypeAsync(
            int type, CancellationToken cancellationToken = default)
        {
            using var connection = connectionFactory.CreateConnection();

            var command = new CommandDefinition(
                SqlByType, new { Type = type }, cancellationToken: cancellationToken);
            var rows = await connection.QueryAsync<LocationDto>(command);

            return Map(rows);
        }

        public async Task<IReadOnlyList<Location>> GetAsync(
            int type, int parent, CancellationToken cancellationToken = default)
        {
            using var connection = connectionFactory.CreateConnection();

            var command = new CommandDefinition(
                SqlByTypeAndParent, new { Type = type, Parent = parent }, cancellationToken: cancellationToken);
            var rows = await connection.QueryAsync<LocationDto>(command);

            return Map(rows);
        }

        private static IReadOnlyList<Location> Map(IEnumerable<LocationDto> rows) =>
            rows
                .Select(d => Location.Reconstitute(d.Id, d.Title, d.TitleEn, d.Code, d.Parent, d.Type))
                .ToList();
    }
}
