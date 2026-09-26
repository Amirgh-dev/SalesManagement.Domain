using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesManagement.API.Features.Customers;
using SalesManagement.Application.Features.Customers.CreateCustomer;

namespace SalesManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class CustomersController : ControllerBase
    {
        private const long MaxRequestBytes = 10L * 1024 * 1024;

        private readonly CreateCustomerHandler _createCustomerHandler;

        public CustomersController(CreateCustomerHandler createCustomerHandler)
        {
            _createCustomerHandler = createCustomerHandler;
        }

        [HttpPost]
        [AllowAnonymous]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(MaxRequestBytes)]
        [RequestFormLimits(MultipartBodyLengthLimit = MaxRequestBytes)]
        [ProducesResponseType(typeof(CreateCustomerResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<CreateCustomerResult>> Create(
            [FromForm] CreateCustomerRequest request, CancellationToken cancellationToken)
        {
            await using var licenseStream = request.LicenseImage?.OpenReadStream();
            var licenseExt = request.LicenseImage is null
                ? null
                : Path.GetExtension(request.LicenseImage.FileName);

            var command = new CreateCustomerCommand(
                request.PartyId,
                request.VisitorCode,
                request.FirstName, request.LastName, request.NationalCode,
                request.LandlinePhone, request.MobilePhone, request.MessengerPhone,
                request.Address, request.ZipCode, request.LocationRef, request.Geolocation,
                request.DateBirth, request.Gender,
                request.HistoryElectricalActivity, request.ShopOperationalHistory,
                request.TypeOwnership, request.AreaSpecialization,
                request.StoreName,
                licenseStream, licenseExt);

            var result = await _createCustomerHandler.Handle(command, cancellationToken);
            return Ok(result);
        }
    }
}
