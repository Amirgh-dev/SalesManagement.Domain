using System.ComponentModel.DataAnnotations;

namespace SalesManagement.API.Features.Auth.PartyLogin
{
    public record PartyLoginRequest(
        [Required]
        [RegularExpression(@"^09\d{9}$", ErrorMessage = "شماره تماس معتبر نیست")]
        string Phone,

        [Required]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "کد ملی باید ۱۰ رقم باشد")]
        string NationalCode);
}
