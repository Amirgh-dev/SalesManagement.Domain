using System.Net;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SalesManagement.Application.Common.Interfaces;
using SalesManagement.Domain.Features.Auth.PartyLogin;
using SalesManagement.Domain.Features.Invoices;
using SalesManagement.Domain.Features.Payments;

namespace SalesManagement.Tests.Features.Payments
{
    public static class TestSettings
    {
        public const string MerchantId = "22222222-2222-2222-2222-222222222222";
        public const string ZarinPalBaseUrl = "https://zarinpal.test";
        public const string CallbackBaseUrl = "https://api.test";
        public const string FrontendResultUrl = "https://app.test";
        public const string SigningKey = "unit-test-only-signing-key-0123456789-abcdefghijklmnop";
        public const string Issuer = "SalesManagement.Tests";
        public const string Audience = "SalesManagement.Tests.Client";

        public static void Apply()
        {
            Environment.SetEnvironmentVariable("JwtSettings__SecretKey", SigningKey);
            Environment.SetEnvironmentVariable("JwtSettings__Issuer", Issuer);
            Environment.SetEnvironmentVariable("JwtSettings__Audience", Audience);
            Environment.SetEnvironmentVariable("ConnectionStrings__Sepidar", "Server=127.0.0.1,1;Database=x;Connect Timeout=1");
            Environment.SetEnvironmentVariable("ConnectionStrings__Sepidyarhesab", "Server=127.0.0.1,1;Database=x;Connect Timeout=1");
            Environment.SetEnvironmentVariable("ConnectionStrings__ZanisServices", "Server=127.0.0.1,1;Database=x;Connect Timeout=1");
            Environment.SetEnvironmentVariable("ZarinPal__MerchantId", MerchantId);
            Environment.SetEnvironmentVariable("ZarinPal__BaseUrl", ZarinPalBaseUrl);
            Environment.SetEnvironmentVariable("ZarinPal__CallbackBaseUrl", CallbackBaseUrl);
            Environment.SetEnvironmentVariable("ZarinPal__FrontendResultUrl", FrontendResultUrl);
        }
    }

    public sealed class ZarinPalStub : HttpMessageHandler
    {
        public const string RequestSuccessJson =
            """{"data":{"code":100,"message":"Success","authority":"AUTH-STUB-1","fee_type":"Merchant","fee":1000},"errors":[]}""";

        public const string VerifyPaidJson =
            """{"data":{"code":100,"message":"Paid","card_hash":"HASH","card_pan":"603799******1234","ref_id":900001,"fee_type":"Merchant","fee":1000},"errors":[]}""";

        public const string ErrorJson =
            """{"data":[],"errors":{"code":-9,"message":"Validation error","validations":[]}}""";

        public const string VerifyNotPaidJson =
            """{"data":[],"errors":{"code":-51,"message":"Session is not valid, session is not active paid try."}}""";

        private int _authoritySequence;

        public List<(string Path, string Body)> Calls { get; } = [];
        public Func<HttpResponseMessage>? OnRequest { get; set; }
        public Func<HttpResponseMessage>? OnVerify { get; set; }

        public int RequestCalls => Calls.Count(c => c.Path.EndsWith("/request.json", StringComparison.Ordinal));
        public int VerifyCalls => Calls.Count(c => c.Path.EndsWith("/verify.json", StringComparison.Ordinal));

        public static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK) =>
            new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

        public void Reset()
        {
            Calls.Clear();
            OnRequest = null;
            OnVerify = null;
            _authoritySequence = 0;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
            var path = request.RequestUri!.AbsolutePath;
            Calls.Add((path, body));

            if (path.EndsWith("/request.json", StringComparison.Ordinal))
            {
                if (OnRequest is not null) return OnRequest();
                var authority = $"AUTH-STUB-{Interlocked.Increment(ref _authoritySequence)}";
                return Json(RequestSuccessJson.Replace("AUTH-STUB-1", authority));
            }

            if (path.EndsWith("/verify.json", StringComparison.Ordinal))
                return OnVerify is not null ? OnVerify() : Json(VerifyPaidJson);

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }
    }

    public sealed class InMemoryRevokedTokenStore : IRevokedTokenStore
    {
        public Task RevokeAsync(Guid jti, DateTimeOffset expiresAt, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<bool> IsRevokedAsync(Guid jti, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
    }

    public class PaymentsApiFactory : WebApplicationFactory<Program>
    {
        static PaymentsApiFactory() => TestSettings.Apply();

        internal FakePaymentRepository Payments { get; } = new();
        internal FakeInvoiceRepository Invoices { get; } = new();
        public ZarinPalStub ZarinPal { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureTestServices(services =>
            {
                foreach (var hosted in services.Where(d => d.ServiceType == typeof(IHostedService)).ToList())
                    services.Remove(hosted);

                ReplaceWith<IPaymentRepository>(services, Payments);
                ReplaceWith<IInvoicePayableRepository>(services, Invoices);
                ReplaceWith<IRevokedTokenStore>(services, new InMemoryRevokedTokenStore());

                services.AddHttpClient(nameof(IPaymentGateway))
                    .ConfigurePrimaryHttpMessageHandler(() => ZarinPal);
            });
        }

        public void Reset()
        {
            Payments.Clear();
            Invoices.Clear();
            ZarinPal.Reset();
        }

        public HttpClient CreateApiClient(int? partyId = null)
        {
            var client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            if (partyId is not null)
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", CreateToken(partyId.Value));
            return client;
        }

        public string CreateToken(int partyId)
        {
            using var scope = Services.CreateScope();
            return scope.ServiceProvider.GetRequiredService<ITokenGenerator>()
                .GenerateAccessToken(partyId, "09000000000");
        }

        private static void ReplaceWith<T>(IServiceCollection services, T instance)
            where T : class
        {
            foreach (var descriptor in services.Where(d => d.ServiceType == typeof(T)).ToList())
                services.Remove(descriptor);
            services.AddSingleton(instance);
        }
    }

    public class RealWiringFactory : WebApplicationFactory<Program>
    {
        static RealWiringFactory() => TestSettings.Apply();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureTestServices(services =>
            {
                foreach (var hosted in services.Where(d => d.ServiceType == typeof(IHostedService)).ToList())
                    services.Remove(hosted);
            });
        }
    }

    [CollectionDefinition(Name)]
    public class PaymentsApiCollection : ICollectionFixture<PaymentsApiFactory>
    {
        public const string Name = "PaymentsApi";
    }
}
