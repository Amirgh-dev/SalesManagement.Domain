using Microsoft.Extensions.Logging;
using SalesManagement.Domain.Features.PartyProfiles;

namespace SalesManagement.Application.Features.PartyProfiles.QueryProfiles
{
    public class QueryProfilesHandler(
        IProfileQueryRepository profileQueryRepository,
        ILogger<QueryProfilesHandler> logger)
    {
        public async Task<QueryProfilesResult> Handle(
            QueryProfilesCommand cmd, CancellationToken cancellationToken = default)
        {
            var query = ProfileQuery.Create(
                cmd.Id,
                cmd.Scope,
                cmd.Fields,
                cmd.SearchTerm,
                cmd.SearchFields,
                cmd.OrderBy,
                cmd.Descending,
                cmd.Skip,
                cmd.Take,
                cmd.Page,
                cmd.PageSize);

            var (total, profiles, posts, comments) =
                await profileQueryRepository.QueryAsync(query, cancellationToken);

            logger.LogInformation(
                "کوئری پروفایل: ریشه={Primary}، id={Id}، total={Total}، پروفایل={P}، پست={Po}، کامنت={C}",
                query.Scope.Primary, query.Id, total,
                profiles?.Count ?? 0, posts?.Count ?? 0, comments?.Count ?? 0);

            return new QueryProfilesResult(total, profiles, posts, comments);
        }
    }
}
