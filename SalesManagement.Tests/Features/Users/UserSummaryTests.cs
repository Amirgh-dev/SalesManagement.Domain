using SalesManagement.Domain.Features.Users;

namespace SalesManagement.Tests.Features.Users
{
    public class UserSummaryTests
    {
        [Fact]
        public void Balance_is_debit_minus_credit()
            => Assert.Equal(300m, new UserSummary(1, "a b", 1000m, 700m).Balance);

        [Fact]
        public void Negative_balance_means_creditor()
            => Assert.Equal(-500m, new UserSummary(1, "a b", 200m, 700m).Balance);

        [Fact]
        public void Zero_movements_give_zero_balance()
            => Assert.Equal(0m, new UserSummary(1, "a b", 0m, 0m).Balance);
    }
}
