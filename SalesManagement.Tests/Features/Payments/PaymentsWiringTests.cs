using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SalesManagement.API.Controllers;
using SalesManagement.Application.Common.Interfaces;
using SalesManagement.Application.Features.Payments.PayInvoice;
using SalesManagement.Application.Features.Payments.RequestPayment;
using SalesManagement.Application.Features.Payments.VerifyPayment;
using SalesManagement.Domain.Features.Invoices;
using SalesManagement.Domain.Features.Payments;
using SalesManagement.Infrastructure.Payments;

namespace SalesManagement.Tests.Features.Payments
{
    public class PaymentsWiringTests(RealWiringFactory factory) : IClassFixture<RealWiringFactory>
    {
        [Fact]
        public void Payment_repository_resolves_to_the_dapper_implementation()
        {
            using var scope = factory.Services.CreateScope();

            Assert.IsType<SalesManagement.Persistence.Features.Payments.PaymentRepository>(
                scope.ServiceProvider.GetRequiredService<IPaymentRepository>());
        }

        [Fact]
        public void Invoice_payable_repository_resolves_to_the_sepidar_implementation()
        {
            using var scope = factory.Services.CreateScope();

            Assert.IsType<SalesManagement.Persistence.Features.Invoices.PayInvoice.InvoicePayableRepository>(
                scope.ServiceProvider.GetRequiredService<IInvoicePayableRepository>());
        }

        [Fact]
        public void Gateway_resolves_to_zarinpal()
        {
            using var scope = factory.Services.CreateScope();

            var gateway = scope.ServiceProvider.GetRequiredService<IPaymentGateway>();

            Assert.IsType<ZarinPalGateway>(gateway);
            Assert.Equal("ZarinPal", gateway.Name);
        }

        [Fact]
        public void Callback_url_provider_is_registered_and_uses_the_configured_base_url()
        {
            using var scope = factory.Services.CreateScope();
            var id = Guid.NewGuid();

            var provider = scope.ServiceProvider.GetRequiredService<IPaymentCallbackUrlProvider>();

            Assert.IsType<PaymentCallbackUrlProvider>(provider);
            Assert.Equal($"{TestSettings.CallbackBaseUrl}/api/payments/callback?paymentId={id}", provider.Build(id));
        }

        [Fact]
        public void Zarinpal_settings_are_bound_from_configuration()
        {
            using var scope = factory.Services.CreateScope();

            var settings = scope.ServiceProvider.GetRequiredService<IOptions<ZarinPalSettings>>().Value;

            Assert.Equal(TestSettings.MerchantId, settings.MerchantId);
            Assert.Equal(TestSettings.ZarinPalBaseUrl, settings.BaseUrl);
            Assert.Equal(TestSettings.CallbackBaseUrl, settings.CallbackBaseUrl);
            Assert.Equal(TestSettings.FrontendResultUrl, settings.FrontendResultUrl);
        }

        [Fact]
        public void All_three_payment_handlers_are_registered()
        {
            using var scope = factory.Services.CreateScope();
            var sp = scope.ServiceProvider;

            Assert.NotNull(sp.GetRequiredService<RequestPaymentHandler>());
            Assert.NotNull(sp.GetRequiredService<VerifyPaymentHandler>());
            Assert.NotNull(sp.GetRequiredService<PayInvoiceHandler>());
        }

        [Fact]
        public void Handlers_are_scoped_one_instance_per_request_scope()
        {
            using var first = factory.Services.CreateScope();
            using var second = factory.Services.CreateScope();

            var a1 = first.ServiceProvider.GetRequiredService<RequestPaymentHandler>();
            var a2 = first.ServiceProvider.GetRequiredService<RequestPaymentHandler>();
            var b = second.ServiceProvider.GetRequiredService<RequestPaymentHandler>();

            Assert.Same(a1, a2);
            Assert.NotSame(a1, b);
        }

        [Fact]
        public void Payments_controller_can_be_activated_with_the_registered_services()
        {
            using var scope = factory.Services.CreateScope();

            var controller = ActivatorUtilities.CreateInstance<PaymentsController>(scope.ServiceProvider);

            Assert.NotNull(controller);
        }

        [Fact]
        public async Task Swagger_document_lists_the_three_payment_routes_without_conflicts()
        {
            using var client = factory.CreateClient();

            var response = await client.GetAsync("/swagger/v1/swagger.json");
            var json = await response.Content.ReadAsStringAsync();

            Assert.True(response.IsSuccessStatusCode, json);
            Assert.Contains("\"/api/Payments/request\"", json);
            Assert.Contains("\"/api/Payments/invoice\"", json);
            Assert.Contains("\"/api/Payments/callback\"", json);
        }
    }
}
