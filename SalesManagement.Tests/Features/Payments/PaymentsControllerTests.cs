using System.Net;
using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SalesManagement.API.Controllers;
using SalesManagement.API.Features.Payments;
using SalesManagement.Application.Common.Interfaces;
using SalesManagement.Application.Features.Payments.PayInvoice;
using SalesManagement.Application.Features.Payments.RequestPayment;
using SalesManagement.Application.Features.Payments.VerifyPayment;
using SalesManagement.Domain.Common.Exceptions;
using SalesManagement.Domain.Enums;
using SalesManagement.Domain.Features.Invoices;
using SalesManagement.Infrastructure.Payments;

namespace SalesManagement.Tests.Features.Payments
{
    public class PaymentsControllerTests
    {
        private readonly FakePaymentRepository _payments = new();
        private readonly FakePaymentGateway _gateway = new();
        private readonly FakeInvoiceRepository _invoices = new();

        private PaymentsController CreateController(
            int? partyId = 7823,
            string? claimValue = null,
            IPAddress? remoteIp = null,
            string frontendUrl = "https://app.test")
        {
            var requestHandler = new RequestPaymentHandler(
                _payments, _gateway, new FakeCallbackUrlProvider(), NullLogger<RequestPaymentHandler>.Instance);
            var verifyHandler = new VerifyPaymentHandler(
                _payments, _gateway, NullLogger<VerifyPaymentHandler>.Instance);
            var payInvoiceHandler = new PayInvoiceHandler(_invoices, _payments, requestHandler);

            var controller = new PaymentsController(
                requestHandler,
                verifyHandler,
                payInvoiceHandler,
                Options.Create(new ZarinPalSettings { FrontendResultUrl = frontendUrl }));

            var http = new DefaultHttpContext();
            http.Connection.RemoteIpAddress = remoteIp;

            var value = claimValue ?? partyId?.ToString();
            if (value is not null)
            {
                http.User = new ClaimsPrincipal(
                    new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, value)], "test"));
            }

            controller.ControllerContext = new ControllerContext { HttpContext = http };
            return controller;
        }

        private static RequestPaymentRequest PaymentBody(long amount = 250_000) =>
            new() { Amount = amount, Description = "desc", Channel = "web" };

        [Fact]
        public async Task RequestPayment_returns_ok_with_the_handler_result()
        {
            var controller = CreateController();

            var action = await controller.RequestPayment(PaymentBody(), CancellationToken.None);

            var ok = Assert.IsType<OkObjectResult>(action.Result);
            var result = Assert.IsType<RequestPaymentResult>(ok.Value);
            Assert.Equal("https://pay.example/AUTH-1", result.PaymentUrl);
            Assert.Equal(Assert.Single(_payments.All).Id, result.PaymentId);
        }

        [Fact]
        public async Task RequestPayment_uses_the_party_from_the_token_claim()
        {
            var controller = CreateController(partyId: 555);

            await controller.RequestPayment(PaymentBody(), CancellationToken.None);

            Assert.Equal(555, Assert.Single(_payments.All).PartyRef);
        }

        [Fact]
        public async Task RequestPayment_never_sets_table_or_target()
        {
            var controller = CreateController();

            await controller.RequestPayment(PaymentBody(), CancellationToken.None);

            var stored = Assert.Single(_payments.All);
            Assert.Null(stored.TableName);
            Assert.Null(stored.TargetRef);
        }

        [Fact]
        public async Task RequestPayment_stores_the_remote_ip_address()
        {
            var controller = CreateController(remoteIp: IPAddress.Parse("203.0.113.5"));

            await controller.RequestPayment(PaymentBody(), CancellationToken.None);

            Assert.Equal("203.0.113.5", Assert.Single(_payments.All).IpAddress);
        }

        [Fact]
        public async Task RequestPayment_stores_an_ipv6_remote_address()
        {
            var controller = CreateController(remoteIp: IPAddress.IPv6Loopback);

            await controller.RequestPayment(PaymentBody(), CancellationToken.None);

            Assert.Equal("::1", Assert.Single(_payments.All).IpAddress);
        }

        [Fact]
        public async Task RequestPayment_falls_back_to_a_placeholder_when_ip_is_unknown()
        {
            var controller = CreateController(remoteIp: null);

            await controller.RequestPayment(PaymentBody(), CancellationToken.None);

            Assert.Equal("0.0.0.0", Assert.Single(_payments.All).IpAddress);
        }

        [Fact]
        public async Task RequestPayment_passes_amount_description_and_channel()
        {
            var controller = CreateController();

            await controller.RequestPayment(
                new RequestPaymentRequest { Amount = 999, Description = "شارژ", Channel = "mobile" },
                CancellationToken.None);

            var stored = Assert.Single(_payments.All);
            Assert.Equal(999, stored.Amount);
            Assert.Equal("شارژ", stored.Description);
            Assert.Equal("mobile", stored.Channel);
        }

        [Fact]
        public async Task RequestPayment_without_a_missing_amount_treats_it_as_zero_and_domain_rejects()
        {
            var controller = CreateController();

            await Assert.ThrowsAsync<DomainException>(() =>
                controller.RequestPayment(
                    new RequestPaymentRequest { Amount = null, Description = "d", Channel = "web" },
                    CancellationToken.None));

            Assert.Empty(_payments.All);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("abc")]
        [InlineData("0")]
        [InlineData("-4")]
        [InlineData("")]
        public async Task RequestPayment_with_a_missing_or_invalid_party_claim_is_unauthenticated(string? claim)
        {
            var controller = CreateController(partyId: null, claimValue: claim);

            await Assert.ThrowsAsync<AuthenticationException>(() =>
                controller.RequestPayment(PaymentBody(), CancellationToken.None));

            Assert.Empty(_payments.All);
            Assert.Equal(0, _gateway.RequestCalls);
        }

        [Fact]
        public async Task PayInvoice_returns_ok_and_uses_token_party_and_ip()
        {
            _invoices.Add(new PayableInvoice(10, 77, 500_000), ownerPartyRef: 7823);
            var controller = CreateController(remoteIp: IPAddress.Parse("198.51.100.9"));

            var action = await controller.PayInvoice(
                new PayInvoiceRequest { InvoiceId = 10, Channel = "web" }, CancellationToken.None);

            Assert.IsType<OkObjectResult>(action.Result);
            var stored = Assert.Single(_payments.All);
            Assert.Equal(7823, stored.PartyRef);
            Assert.Equal("198.51.100.9", stored.IpAddress);
            Assert.Equal(500_000, stored.Amount);
        }

        [Fact]
        public async Task PayInvoice_for_someone_elses_invoice_throws_not_found_domain_error()
        {
            _invoices.Add(new PayableInvoice(10, 77, 500_000), ownerPartyRef: 1);
            var controller = CreateController(partyId: 7823);

            var ex = await Assert.ThrowsAsync<DomainException>(() =>
                controller.PayInvoice(new PayInvoiceRequest { InvoiceId = 10, Channel = "web" }, CancellationToken.None));

            Assert.Equal("فاکتور پیدا نشد", ex.Message);
        }

        [Fact]
        public async Task PayInvoice_without_a_party_claim_is_unauthenticated()
        {
            _invoices.Add(new PayableInvoice(10, 77, 500_000), ownerPartyRef: 7823);
            var controller = CreateController(partyId: null);

            await Assert.ThrowsAsync<AuthenticationException>(() =>
                controller.PayInvoice(new PayInvoiceRequest { InvoiceId = 10, Channel = "web" }, CancellationToken.None));
        }

        private async Task<(Guid Id, string Authority)> SeedSentAsync()
        {
            var controller = CreateController();
            var action = await controller.RequestPayment(PaymentBody(), CancellationToken.None);
            var id = ((RequestPaymentResult)((OkObjectResult)action.Result!).Value!).PaymentId;
            return (id, _payments.All.Single(p => p.Id == id).Authority!);
        }

        [Fact]
        public async Task Callback_success_redirects_to_the_frontend_result_page()
        {
            var (id, authority) = await SeedSentAsync();
            var controller = CreateController();

            var result = await controller.Callback(id, "OK", authority, CancellationToken.None);

            var redirect = Assert.IsType<RedirectResult>(result);
            var tracking = _payments.All.Single().TrackingCode;
            Assert.Equal($"https://app.test/payment-result?success=true&trackingCode={tracking}", redirect.Url);
            Assert.False(redirect.Permanent);
        }

        [Fact]
        public async Task Callback_cancel_redirects_with_success_false()
        {
            var (id, authority) = await SeedSentAsync();
            var controller = CreateController();

            var result = await controller.Callback(id, "NOK", authority, CancellationToken.None);

            var redirect = Assert.IsType<RedirectResult>(result);
            Assert.StartsWith("https://app.test/payment-result?success=false&trackingCode=", redirect.Url);
            Assert.Equal(PaymentState.CanceledByUser, _payments.All.Single().State);
        }

        [Fact]
        public async Task Callback_null_status_is_treated_as_cancel()
        {
            var (id, authority) = await SeedSentAsync();
            var controller = CreateController();

            var result = await controller.Callback(id, null, authority, CancellationToken.None);

            Assert.Contains("success=false", Assert.IsType<RedirectResult>(result).Url);
            Assert.Equal(PaymentState.CanceledByUser, _payments.All.Single().State);
        }

        [Fact]
        public async Task Callback_domain_error_redirects_to_failure_without_tracking_code()
        {
            var controller = CreateController();

            var result = await controller.Callback(Guid.NewGuid(), "OK", "x", CancellationToken.None);

            Assert.Equal(
                "https://app.test/payment-result?success=false", Assert.IsType<RedirectResult>(result).Url);
        }

        [Theory]
        [InlineData("https://app.test/")]
        [InlineData("https://app.test///")]
        [InlineData("https://app.test")]
        public async Task Callback_trims_trailing_slashes_from_the_frontend_url(string frontend)
        {
            var controller = CreateController(frontendUrl: frontend);

            var result = await controller.Callback(Guid.NewGuid(), "OK", "x", CancellationToken.None);

            Assert.Equal(
                "https://app.test/payment-result?success=false", Assert.IsType<RedirectResult>(result).Url);
        }

        [Fact]
        public async Task Callback_does_not_swallow_unexpected_errors()
        {
            var (id, authority) = await SeedSentAsync();
            _gateway.VerifyException = new HttpRequestException("down");
            var controller = CreateController();

            await Assert.ThrowsAsync<HttpRequestException>(() =>
                controller.Callback(id, "OK", authority, CancellationToken.None));
        }

        [Fact]
        public async Task Callback_does_not_require_a_logged_in_user()
        {
            var controller = CreateController(partyId: null);

            var result = await controller.Callback(Guid.NewGuid(), "OK", "x", CancellationToken.None);

            Assert.IsType<RedirectResult>(result);
        }

        [Fact]
        public async Task Callback_forwards_the_cancellation_token_to_the_gateway()
        {
            var (id, authority) = await SeedSentAsync();
            using var cts = new CancellationTokenSource();
            var controller = CreateController();

            await controller.Callback(id, "OK", authority, cts.Token);

            Assert.Equal(cts.Token, _gateway.LastVerifyToken);
        }

        [Fact]
        public async Task RequestPayment_forwards_the_cancellation_token_to_the_gateway()
        {
            using var cts = new CancellationTokenSource();
            var controller = CreateController();

            await controller.RequestPayment(PaymentBody(), cts.Token);

            Assert.Equal(cts.Token, _gateway.LastRequestToken);
        }

        private static MethodInfo Action(string name) =>
            typeof(PaymentsController).GetMethod(name, BindingFlags.Public | BindingFlags.Instance)!;

        [Fact]
        public void Controller_is_an_api_controller_routed_at_api_payments()
        {
            var type = typeof(PaymentsController);

            Assert.NotNull(type.GetCustomAttribute<ApiControllerAttribute>());
            Assert.Equal("api/[controller]", type.GetCustomAttribute<RouteAttribute>()!.Template);
        }

        [Fact]
        public void Controller_class_itself_is_not_authorize_so_callback_can_be_anonymous()
        {
            Assert.Null(typeof(PaymentsController).GetCustomAttribute<AuthorizeAttribute>());
        }

        [Theory]
        [InlineData(nameof(PaymentsController.RequestPayment), "request")]
        [InlineData(nameof(PaymentsController.PayInvoice), "invoice")]
        public void Payment_creating_actions_are_post_and_require_authorization(string action, string route)
        {
            var method = Action(action);

            Assert.Equal(route, method.GetCustomAttribute<HttpPostAttribute>()!.Template);
            Assert.NotNull(method.GetCustomAttribute<AuthorizeAttribute>());
            Assert.Null(method.GetCustomAttribute<AllowAnonymousAttribute>());
        }

        [Fact]
        public void Callback_is_a_get_and_allows_anonymous_access()
        {
            var method = Action(nameof(PaymentsController.Callback));

            Assert.Equal("callback", method.GetCustomAttribute<HttpGetAttribute>()!.Template);
            Assert.NotNull(method.GetCustomAttribute<AllowAnonymousAttribute>());
        }

        [Fact]
        public void Every_action_except_callback_requires_authorization()
        {
            var actions = typeof(PaymentsController)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => m.GetCustomAttributes().OfType<HttpMethodAttribute>().Any())
                .ToList();

            Assert.Equal(3, actions.Count);
            foreach (var action in actions.Where(a => a.Name != nameof(PaymentsController.Callback)))
                Assert.NotNull(action.GetCustomAttribute<AuthorizeAttribute>());
        }

        [Fact]
        public void Body_parameters_are_bound_from_the_body_and_callback_from_the_query()
        {
            Assert.NotNull(Action(nameof(PaymentsController.RequestPayment)).GetParameters()[0].GetCustomAttribute<FromBodyAttribute>());
            Assert.NotNull(Action(nameof(PaymentsController.PayInvoice)).GetParameters()[0].GetCustomAttribute<FromBodyAttribute>());

            var callbackParams = Action(nameof(PaymentsController.Callback)).GetParameters();
            Assert.All(callbackParams.Take(3), p => Assert.NotNull(p.GetCustomAttribute<FromQueryAttribute>()));
        }
    }
}
