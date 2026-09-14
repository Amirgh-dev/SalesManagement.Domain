using Microsoft.Extensions.Logging;
using SalesManagement.Application.Common.Interfaces;
using SalesManagement.Domain.Common.Exceptions;
using SalesManagement.Domain.Features.Customers;

namespace SalesManagement.Application.Features.Customers.CreateCustomer
{
    public class CreateCustomerHandler(
        ICustomerRepository customerRepository,
        IFileStorage fileStorage,
        ILogger<CreateCustomerHandler> logger)
    {
        private const string PartyFolder = "Party";
        private const int MaxAttempts = 3;

        public async Task<CreateCustomerResult> Handle(
            CreateCustomerCommand cmd, CancellationToken cancellationToken = default)
        {
            var newCustomer = NewCustomer.Create(
                cmd.PartyId, cmd.FirstName, cmd.LastName, cmd.NationalCode,
                cmd.LandlinePhone, cmd.MobilePhone, cmd.MessengerPhone,
                cmd.Address, cmd.ZipCode, cmd.LocationRef, cmd.Geolocation,
                new NewCustomerExtra(
                    cmd.DateBirth, cmd.Gender,
                    cmd.HistoryElectricalActivity, cmd.ShopOperationalHistory,
                    cmd.TypeOwnership, cmd.AreaSpecialization, cmd.StoreName));

            string? savedLicense = null;
            if (cmd.LicenseImageContent is not null)
            {
                savedLicense = await fileStorage.SaveAsync(
                    cmd.LicenseImageContent, PartyFolder,
                    cmd.LicenseImageExtension ?? ".jpg", cancellationToken);
                newCustomer = newCustomer.WithLicenseFile(savedLicense);
            }

            try
            {
                if (newCustomer.IsUpdate)
                {
                    var removedFileNames = await customerRepository.UpdateAsync(
                        newCustomer, cmd.VisitorCode, cancellationToken);

                    foreach (var fileName in removedFileNames)
                        await fileStorage.DeleteAsync(PartyFolder, fileName, CancellationToken.None);

                    logger.LogInformation("مشتری به‌روزرسانی شد: PartyId={PartyId}", newCustomer.PartyId);
                    return new CreateCustomerResult(newCustomer.PartyId!.Value);
                }

                for (var attempt = 1; ; attempt++)
                {
                    try
                    {
                        var partyId = await customerRepository.CreateAsync(
                            newCustomer, cmd.VisitorCode, cancellationToken);

                        logger.LogInformation("مشتری جدید ساخته شد: PartyId={PartyId}", partyId);
                        return new CreateCustomerResult(partyId);
                    }
                    catch (IdGenerationCollisionException) when (attempt < MaxAttempts)
                    {
                        logger.LogWarning(
                            "برخورد در تولید شناسه هنگام ثبت مشتری، تلاش دوباره ({Attempt}/{Max})",
                            attempt, MaxAttempts);
                    }
                }
            }
            catch
            {
                if (savedLicense is not null)
                    await fileStorage.DeleteAsync(PartyFolder, savedLicense, CancellationToken.None);
                throw;
            }
        }
    }
}