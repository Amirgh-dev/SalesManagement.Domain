using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesManagement.API.Common;
using SalesManagement.Application.Features.Quotations.GetCustomerQuotations;
using SalesManagement.Application.Features.Quotations.GetVendorQuotationById;
using SalesManagement.Application.Features.Quotations.GetVendorQuotations;

namespace SalesManagement.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    [SuppressMessage("Major Code Smell", "S6960:Controllers should not have mixed responsibilities",
        Justification = "All actions operate on the same Quotations resource (list-mine, get-by-id, list-by-customer).")]
    public class QuotationsController : ControllerBase
    {
        private readonly GetVendorQuotationsHandler _handler;
        private readonly GetVendorQuotationByIdHandler _byIdHandler;
        private readonly GetCustomerQuotationsHandler _byCustomerHandler;

        public QuotationsController(
            GetVendorQuotationsHandler handler,
            GetVendorQuotationByIdHandler byIdHandler,
            GetCustomerQuotationsHandler byCustomerHandler)
        {
            _handler = handler;
            _byIdHandler = byIdHandler;
            _byCustomerHandler = byCustomerHandler;
        }

        [HttpPost]
        [ProducesResponseType(typeof(GetVendorQuotationsResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<GetVendorQuotationsResult>> GetMine(
            CancellationToken cancellationToken)
        {
            var partyId = User.GetPartyId();

            var result = await _handler.Handle(
                new GetVendorQuotationsQuery(partyId), cancellationToken);
            return Ok(result);
        }

        [HttpPost("{id:int}")]
        [ProducesResponseType(typeof(GetVendorQuotationByIdResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<GetVendorQuotationByIdResult>> GetById(
            int id, CancellationToken cancellationToken)
        {
            var partyId = User.GetPartyId();

            var result = await _byIdHandler.Handle(
                new GetVendorQuotationByIdQuery(partyId, id), cancellationToken);
            return Ok(result);
        }

        // POST api/quotations/customers/{customerPartyRef} — پیش‌فاکتورهای یک مشتری
        [HttpPost("customers/{customerPartyRef:int}")]
        [ProducesResponseType(typeof(GetCustomerQuotationsResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<GetCustomerQuotationsResult>> GetByCustomer(
            int customerPartyRef, CancellationToken cancellationToken)
        {
            var partyId = User.GetPartyId();

            var result = await _byCustomerHandler.Handle(
                new GetCustomerQuotationsQuery(partyId, customerPartyRef), cancellationToken);
            return Ok(result);
        }
    }
}
