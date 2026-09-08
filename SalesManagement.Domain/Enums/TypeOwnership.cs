using System.ComponentModel;

namespace SalesManagement.Domain.Enums
{
    public enum TypeOwnership
    {
        [Description("استیجاری")] Rental = 1,
        [Description("سرقفلی")] KeyMoney = 2,
        [Description("مالکیت و سرقفلی")] OwnershipAndKeyMoney = 3
    }
}