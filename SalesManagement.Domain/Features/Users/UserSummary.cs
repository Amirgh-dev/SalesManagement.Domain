namespace SalesManagement.Domain.Features.Users
{
    public sealed record UserSummary(
        int PartyId, string FullName, decimal TotalDebit, decimal TotalCredit)
    {
        public decimal Balance => TotalDebit - TotalCredit;
    }
}
