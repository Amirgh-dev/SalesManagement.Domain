using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesManagement.API.Common;
using SalesManagement.Application.Features.Invoices.GetCustomerInvoices;
using SalesManagement.Application.Features.Invoices.GetVendorInvoiceById;
using SalesManagement.Application.Features.Invoices.GetVendorInvoices;

namespace SalesManagement.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    [SuppressMessage("Major Code Smell", "S6960:Controllers should not have mixed responsibilities",
        Justification = "All actions operate on the same Invoices resource (list-mine, get-by-id, list-by-customer).")]
    public class InvoicesController : ControllerBase
    {
        private readonly GetVendorInvoicesHandler _handler;
        private readonly GetVendorInvoiceByIdHandler _byIdHandler;
        private readonly GetCustomerInvoicesHandler _byCustomerHandler;

        public InvoicesController(
            GetVendorInvoicesHandler handler,
            GetVendorInvoiceByIdHandler byIdHandler,
            GetCustomerInvoicesHandler byCustomerHandler)
        {
            _handler = handler;
            _byIdHandler = byIdHandler;
            _byCustomerHandler = byCustomerHandler;
        }

        [HttpPost]
        [ProducesResponseType(typeof(GetVendorInvoicesResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<GetVendorInvoicesResult>> GetMine(
            CancellationToken cancellationToken)
        {
            var partyId = User.GetPartyId();

            var result = await _handler.Handle(
                new GetVendorInvoicesQuery(partyId), cancellationToken);
            return Ok(result);
        }

        [HttpPost("{id:int}")]
        [ProducesResponseType(typeof(GetVendorInvoiceByIdResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<GetVendorInvoiceByIdResult>> GetById(
            int id, CancellationToken cancellationToken)
        {
            var partyId = User.GetPartyId();

            var result = await _byIdHandler.Handle(
                new GetVendorInvoiceByIdQuery(partyId, id), cancellationToken);
            return Ok(result);
        }

        [HttpPost("customers/{customerPartyRef:int}")]
        [ProducesResponseType(typeof(GetCustomerInvoicesResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<GetCustomerInvoicesResult>> GetByCustomer(
            int customerPartyRef, CancellationToken cancellationToken)
        {
            var partyId = User.GetPartyId();

            var result = await _byCustomerHandler.Handle(
                new GetCustomerInvoicesQuery(partyId, customerPartyRef), cancellationToken);
            return Ok(result);
        }
    }
}
