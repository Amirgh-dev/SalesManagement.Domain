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
                cmd.PartyRef,
                cmd.Scope,
                cmd.Fields,
                cmd.SearchTerm,
                cmd.SearchFields,
                cmd.OrderBy,
                cmd.Descending,
                cmd.Skip,
                cmd.Take,
                cmd.Page,
                cmd.PageSize,
                cmd.ProvinceRef,
                cmd.CountyRef);

            var (total, profiles, posts, comments, provinces, counties) =
                await profileQueryRepository.QueryAsync(query, cancellationToken);

            var isAccount = await profileQueryRepository.HasProfileAsync(cmd.RequesterPartyId, cancellationToken);

            logger.LogInformation(
                "کوئری پروفایل: ریشه={Primary}، id={Id}، total={Total}، پروفایل={P}، پست={Po}، کامنت={C}، isAccount={IsAccount}",
                query.Scope.Primary, query.Id, total,
                profiles?.Count ?? 0, posts?.Count ?? 0, comments?.Count ?? 0, isAccount);

            return new QueryProfilesResult(isAccount, total, profiles, posts, comments, provinces, counties);
        }
    }
}
