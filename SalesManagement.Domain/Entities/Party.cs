using SalesManagement.Domain.Common.Exceptions;

namespace SalesManagement.Domain.Entities
{
    public class Party
    {
        public int PartyId { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public string LastName { get; private set; } = string.Empty;
        public string IdentificationCode { get; private set; } = string.Empty;
        public string? Email { get; private set; }
        public bool IsCustomer { get; private set; }
        public bool IsEmployee { get; private set; }
        public bool IsVendor { get; private set; }
        public bool IsInBlacklist { get; private set; }
        public decimal CustomerRemaining { get; private set; }
        public string? DLCode { get; private set; }
        public string? DLTitle { get; private set; }

        private Party() { }

        public static Party Reconstitute(
            int partyId, string name, string lastName, string identificationCode,
            string? email, bool isCustomer, bool isEmployee, bool isVendor,
            bool isInBlacklist, decimal customerRemaining, string? dlCode, string? dlTitle)
        {
            return new Party
            {
                PartyId = partyId,
                Name = name,
                LastName = lastName,
                IdentificationCode = identificationCode,
                Email = email,
                IsCustomer = isCustomer,
                IsEmployee = isEmployee,
                IsVendor = isVendor,
                IsInBlacklist = isInBlacklist,
                CustomerRemaining = customerRemaining,
                DLCode = dlCode,
                DLTitle = dlTitle
            };
        }

        public void EnsureCanLogin()
        {
            if (IsInBlacklist)
                throw new ForbiddenException("دسترسی این کاربر مسدود شده است");
        }

        public string FullName => $"{Name} {LastName}".Trim();
    }
}