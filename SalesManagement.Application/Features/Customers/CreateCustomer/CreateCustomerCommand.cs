using SalesManagement.Domain.Enums;

namespace SalesManagement.Application.Features.Customers.CreateCustomer
{
    public record CreateCustomerCommand(
        string? VisitorCode,
        string? FirstName,
        string? LastName,
        string? NationalCode,
        string? LandlinePhone,
        string? MobilePhone,
        string? MessengerPhone,
        string? Address,
        string? ZipCode,
        int LocationRef,
        string? Geolocation, 
        DateTime? DateBirth,
        bool? Gender,
        string? HistoryElectricalActivity,
        string? ShopOperationalHistory,
        TypeOwnership? TypeOwnership,
        AreaSpecialization? AreaSpecialization,
        Stream? LicenseImageContent,
        string? LicenseImageExtension);
}