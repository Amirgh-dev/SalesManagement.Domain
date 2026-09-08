using System.ComponentModel.DataAnnotations;
using SalesManagement.Domain.Enums;

namespace SalesManagement.API.Features.Customers
{
    public class CreateCustomerRequest
    {
        public string? VisitorCode { get; init; }

        [Required] public string FirstName { get; init; } = "";
        [Required] public string LastName { get; init; } = "";
        [Required] public string NationalCode { get; init; } = "";
        [Required] public string LandlinePhone { get; init; } = "";
        [Required] public string MobilePhone { get; init; } = "";
        [Required] public string MessengerPhone { get; init; } = "";
        [Required] public string Address { get; init; } = "";
        [Required] public string ZipCode { get; init; } = "";
        [Range(1, int.MaxValue, ErrorMessage = "شهر/استان را انتخاب کنید")]
        public int LocationRef { get; init; }
        [Required] public string Geolocation { get; init; } = ""; 

        public DateTime? DateBirth { get; init; }
        public bool? Gender { get; init; }
        public string? HistoryElectricalActivity { get; init; }
        public string? ShopOperationalHistory { get; init; }
        public TypeOwnership? TypeOwnership { get; init; }
        public AreaSpecialization? AreaSpecialization { get; init; }

        public IFormFile? LicenseImage { get; init; } 
    }
}