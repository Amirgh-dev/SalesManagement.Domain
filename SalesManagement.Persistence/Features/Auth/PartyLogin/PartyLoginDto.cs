namespace SalesManagement.Persistence.Features.Auth.PartyLogin
{
    public class PartyLoginDto
    {
        public int PartyId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string IdentificationCode { get; set; } = string.Empty;
        public string? Email { get; set; }
        public bool IsCustomer { get; set; }
        public bool IsEmployee { get; set; }
        public bool IsVendor { get; set; }
        public bool IsInBlacklist { get; set; }
        public decimal CustomerRemaining { get; set; }
        public string? DlCode { get; set; }
        public string? DlTitle { get; set; }
    }
}