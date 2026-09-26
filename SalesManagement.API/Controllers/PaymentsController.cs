using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using SalesManagement.API.Common;
using SalesManagement.API.Features.Payments;
using SalesManagement.Application.Features.Payments.PayInvoice;
using SalesManagement.Application.Features.Payments.RequestPayment;
using SalesManagement.Application.Features.Payments.VerifyPayment;
using SalesManagement.Domain.Common.Exceptions;
using SalesManagement.Infrastructure.Payments;

namespace SalesManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [SuppressMessage("Major Code Smell", "S6960:Controllers should not have mixed responsibilities",
        Justification = "All actions operate on the same Payments resource (request, invoice payment and gateway callback).")]
    public class PaymentsController : ControllerBase
    {
        private const string UnknownIpAddress = "0.0.0.0";
        private const string ResultPagePath = "/payment-result";

        private readonly RequestPaymentHandler _requestHandler;
        private readonly VerifyPaymentHandler _verifyHandler;
        private readonly PayInvoiceHandler _payInvoiceHandler;
        private readonly ZarinPalSettings _settings;

        public PaymentsController(
            RequestPaymentHandler requestHandler,
            VerifyPaymentHandler verifyHandler,
            PayInvoiceHandler payInvoiceHandler,
            IOptions<ZarinPalSettings> settings)
        {
            _requestHandler = requestHandler;
            _verifyHandler = verifyHandler;
            _payInvoiceHandler = payInvoiceHandler;
            _settings = settings.Value;
        }

        [HttpPost("request")]
        [Authorize]
        [ProducesResponseType(typeof(RequestPaymentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<RequestPaymentResult>> RequestPayment(
            [FromBody] RequestPaymentRequest request, CancellationToken cancellationToken)
        {
            var result = await _requestHandler.Handle(
                new RequestPaymentCommand(
                    User.GetPartyId(),
                    null,
                    null,
                    request.Amount.GetValueOrDefault(),
                    request.Description,
                    request.Channel,
                    GetClientIp()),
                cancellationToken);

            return Ok(result);
        }

        [HttpPost("invoice")]
        [Authorize]
        [ProducesResponseType(typeof(RequestPaymentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<RequestPaymentResult>> PayInvoice(
            [FromBody] PayInvoiceRequest request, CancellationToken cancellationToken)
        {
            var result = await _payInvoiceHandler.Handle(
                new PayInvoiceCommand(
                    request.InvoiceId.GetValueOrDefault(), User.GetPartyId(), request.Channel, GetClientIp()),
                cancellationToken);

            return Ok(result);
        }

        [HttpGet("callback")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status302Found)]
        public async Task<IActionResult> Callback(
            [FromQuery] Guid paymentId,
            [FromQuery] string? status,
            [FromQuery] string? authority,
            CancellationToken cancellationToken)
        {
            try
            {
                var result = await _verifyHandler.Handle(
                    new VerifyPaymentCommand(paymentId, status ?? "", authority), cancellationToken);

                return Redirect(BuildResultUrl(result.IsSuccess, result.TrackingCode));
            }
            catch (DomainException)
            {
                return Redirect(BuildResultUrl(false, null));
            }
        }

        private string GetClientIp() =>
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? UnknownIpAddress;

        private string BuildResultUrl(bool success, string? trackingCode)
        {
            var query = new Dictionary<string, string?>
            {
                ["success"] = success ? "true" : "false",
                ["trackingCode"] = trackingCode,
            };

            return QueryHelpers.AddQueryString(_settings.FrontendResultUrl.TrimEnd('/') + ResultPagePath, query);
        }
    }
}
