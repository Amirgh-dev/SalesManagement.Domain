using Microsoft.Extensions.Logging;
using SalesManagement.Application.Common.Interfaces;
using SalesManagement.Domain.Features.Customers;

namespace SalesManagement.Application.Features.Customers.CreateCustomer
{
    public class CreateCustomerHandler(
        ICustomerRepository customerRepository,
        IFileStorage fileStorage,
        ILogger<CreateCustomerHandler> logger)
    {
        private const string LicenseFolder = "Party";

        public async Task<CreateCustomerResult> Handle(
            CreateCustomerCommand cmd, CancellationToken cancellationToken = default)
        {
            var newCustomer = NewCustomer.Create(
                cmd.FirstName, cmd.LastName, cmd.NationalCode,
                cmd.LandlinePhone, cmd.MobilePhone, cmd.MessengerPhone,
                cmd.Address, cmd.ZipCode, cmd.LocationRef, cmd.Geolocation,
                new NewCustomerExtra(
                    cmd.DateBirth, cmd.Gender,
                    cmd.HistoryElectricalActivity, cmd.ShopOperationalHistory,
                    cmd.TypeOwnership, cmd.AreaSpecialization));

            string? savedFileName = null;
            if (cmd.LicenseImageContent is not null)
            {
                savedFileName = await fileStorage.SaveAsync(
                    cmd.LicenseImageContent, LicenseFolder,
                    cmd.LicenseImageExtension ?? ".jpg", cancellationToken);
                newCustomer = newCustomer.WithLicenseFile(savedFileName);
            }

            try
            {
                var partyId = await customerRepository.CreateAsync(
                    newCustomer, cmd.VisitorCode, cancellationToken);

                logger.LogInformation("مشتری جدید ساخته شد: PartyId={PartyId}", partyId);
                return new CreateCustomerResult(partyId);
            }
            catch
            {
                if (savedFileName is not null)
                    await fileStorage.DeleteAsync(LicenseFolder, savedFileName, CancellationToken.None);
                throw;
            }
        }
    }
}