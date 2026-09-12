using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesManagement.API.Common;
using SalesManagement.API.Features.Invoices;
using SalesManagement.Application.Common;
using SalesManagement.Application.Features.Invoices.QueryInvoices;

namespace SalesManagement.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class InvoicesController : ControllerBase
    {
        private readonly QueryInvoicesHandler _handler;

        public InvoicesController(QueryInvoicesHandler handler)
        {
            _handler = handler;
        }

        [HttpPost("[action]")]
        [ProducesResponseType(typeof(QueryInvoicesResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<QueryInvoicesResult>> Query(
            [FromBody] QueryInvoicesRequest request, CancellationToken cancellationToken)
        {
            var partyId = User.GetPartyId();

            var filters = new List<QueryFilterInput>();
            if (request.Id is int id && id > 0)
                filters.Add(new QueryFilterInput("invoiceId", "eq", id.ToString()));
            if (request.CustomerPartyRef is int cpr && cpr > 0)
                filters.Add(new QueryFilterInput("customerPartyRef", "eq", cpr.ToString()));
            if (request.Filters is not null)
                filters.AddRange(request.Filters.Select(f => new QueryFilterInput(f.Field, f.Op, f.Value)));

            var search = request.Search is null
                ? null
                : new QuerySearchInput(request.Search.Term, request.Search.Fields);

            var result = await _handler.Handle(
                new QueryInvoicesCommand(
                    partyId, request.Scope, request.Fields, filters, search,
                    request.OrderBy, request.Descending, request.Skip, request.Take,
                    request.Page, request.PageSize),
                cancellationToken);

            return Ok(result);
        }
    }
}
