using SalesManagement.Application.Features.Auth.PartyLogin;
using SalesManagement.Domain.Common.Exceptions;
using SalesManagement.Domain.Features.Users;

namespace SalesManagement.Application.Features.Users.QueryUser
{
    public record QueryUserCommand(int PartyId);

    public record QueryUserResult(
        int PartyId,
        string FullName,
        string? ProfilePhotoUrl,
        decimal TotalDebit,
        decimal TotalCredit,
        decimal Balance);

    public class QueryUserHandler(
        IUserQueryRepository userQueryRepository,
        ProfilePhotoUrlResolver photoUrlResolver)
    {
        public async Task<QueryUserResult> Handle(
            QueryUserCommand command, CancellationToken cancellationToken = default)
        {
            var summary = await userQueryRepository.GetSummaryAsync(command.PartyId, cancellationToken)
                          ?? throw new NotFoundException("کاربر پیدا نشد");

            var photoUrl = await photoUrlResolver.GetAsync(command.PartyId, cancellationToken);

            return new QueryUserResult(
                summary.PartyId, summary.FullName, photoUrl,
                summary.TotalDebit, summary.TotalCredit, summary.Balance);
        }
    }
}
