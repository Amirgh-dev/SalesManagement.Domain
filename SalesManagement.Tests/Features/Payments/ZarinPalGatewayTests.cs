using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SalesManagement.Infrastructure.Payments;

namespace SalesManagement.Tests.Features.Payments
{
    public class ZarinPalGatewayTests
    {
        private const string BaseUrl = "https://sandbox.example";
        private const string MerchantId = "11111111-1111-1111-1111-111111111111";

        private const string RequestSuccessJson =
            """{"data":{"code":100,"message":"Success","authority":"A0001","fee_type":"Merchant","fee":1000},"errors":[]}""";

        private const string ErrorJson =
            """{"data":[],"errors":{"code":-9,"message":"Validation error","validations":[]}}""";

        private const string VerifyPaidJson =
            """{"data":{"wages":[],"code":100,"message":"Paid","card_hash":"HASH","card_pan":"603799******1234","ref_id":201,"fee_type":"Merchant","fee":1000},"errors":[]}""";

        private const string VerifyAlreadyJson =
            """{"data":{"code":101,"message":"Verified","card_hash":"HASH","card_pan":"603799******1234","ref_id":201,"fee_type":"Merchant","fee":1000},"errors":[]}""";

        private const string VerifyNotPaidJson =
            """{"data":[],"errors":{"code":-51,"message":"Session is not valid, session is not active paid try."}}""";

        private static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK) =>
            new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

        private static ZarinPalGateway CreateGateway(StubHttpHandler handler, string merchantId = MerchantId) =>
            new(
                new HttpClient(handler),
                Options.Create(new ZarinPalSettings { MerchantId = merchantId, BaseUrl = BaseUrl }));

        [Fact]
        public void Name_is_ZarinPal()
        {
            Assert.Equal("ZarinPal", CreateGateway(new StubHttpHandler(_ => Json("{}"))).Name);
        }

        [Fact]
        public async Task Request_success_returns_authority_fee_and_payment_url()
        {
            var gateway = CreateGateway(new StubHttpHandler(_ => Json(RequestSuccessJson)));

            var result = await gateway.RequestAsync(250_000, "desc", "https://cb");

            Assert.True(result.Success);
            Assert.Equal("A0001", result.Authority);
            Assert.Equal($"{BaseUrl}/pg/StartPay/A0001", result.PaymentUrl);
            Assert.Equal(1000, result.Fee);
            Assert.Null(result.ErrorCode);
            Assert.Null(result.ErrorMessage);
        }

        [Fact]
        public async Task Request_posts_expected_body_to_request_endpoint()
        {
            var handler = new StubHttpHandler(_ => Json(RequestSuccessJson));

            await CreateGateway(handler).RequestAsync(250_000, "پرداخت فاکتور", "https://cb/x?paymentId=1");

            var call = Assert.Single(handler.Requests);
            Assert.Equal(HttpMethod.Post, call.Method);
            Assert.Equal($"{BaseUrl}/pg/v4/payment/request.json", call.Uri!.ToString());

            using var doc = JsonDocument.Parse(call.Body);
            var root = doc.RootElement;
            Assert.Equal(MerchantId, root.GetProperty("merchant_id").GetString());
            Assert.Equal(250_000, root.GetProperty("amount").GetInt64());
            Assert.Equal("پرداخت فاکتور", root.GetProperty("description").GetString());
            Assert.Equal("https://cb/x?paymentId=1", root.GetProperty("callback_url").GetString());
        }

        [Fact]
        public async Task Request_error_with_empty_data_array_returns_failure_with_code_and_message()
        {
            var gateway = CreateGateway(new StubHttpHandler(_ => Json(ErrorJson)));

            var result = await gateway.RequestAsync(250_000, "desc", "https://cb");

            Assert.False(result.Success);
            Assert.Null(result.Authority);
            Assert.Null(result.PaymentUrl);
            Assert.Equal("-9", result.ErrorCode);
            Assert.Equal("Validation error", result.ErrorMessage);
        }

        [Fact]
        public async Task Request_error_on_http_400_still_returns_gateway_error()
        {
            var gateway = CreateGateway(new StubHttpHandler(_ => Json(ErrorJson, HttpStatusCode.BadRequest)));

            var result = await gateway.RequestAsync(250_000, "desc", "https://cb");

            Assert.False(result.Success);
            Assert.Equal("-9", result.ErrorCode);
        }

        [Fact]
        public async Task Request_success_code_without_authority_is_failure()
        {
            var gateway = CreateGateway(new StubHttpHandler(_ =>
                Json("""{"data":{"code":100,"message":"Success"},"errors":[]}""")));

            var result = await gateway.RequestAsync(250_000, "desc", "https://cb");

            Assert.False(result.Success);
        }

        [Fact]
        public async Task Request_non_success_code_is_failure()
        {
            var gateway = CreateGateway(new StubHttpHandler(_ =>
                Json("""{"data":{"code":101,"authority":"A0001"},"errors":[]}""")));

            var result = await gateway.RequestAsync(250_000, "desc", "https://cb");

            Assert.False(result.Success);
        }

        [Fact]
        public async Task Request_html_error_page_returns_failure_instead_of_throwing()
        {
            var gateway = CreateGateway(new StubHttpHandler(_ =>
                new HttpResponseMessage(HttpStatusCode.BadGateway)
                {
                    Content = new StringContent("<html>Bad Gateway</html>", Encoding.UTF8, "text/html"),
                }));

            var result = await gateway.RequestAsync(250_000, "desc", "https://cb");

            Assert.False(result.Success);
            Assert.Null(result.Authority);
        }

        [Fact]
        public async Task Request_invalid_json_body_returns_failure_instead_of_throwing()
        {
            var gateway = CreateGateway(new StubHttpHandler(_ => Json("not json at all")));

            var result = await gateway.RequestAsync(250_000, "desc", "https://cb");

            Assert.False(result.Success);
        }

        [Fact]
        public async Task Request_empty_body_returns_failure_instead_of_throwing()
        {
            var gateway = CreateGateway(new StubHttpHandler(_ => Json("")));

            var result = await gateway.RequestAsync(250_000, "desc", "https://cb");

            Assert.False(result.Success);
        }

        [Fact]
        public async Task Request_connection_failure_returns_network_error()
        {
            var gateway = CreateGateway(new StubHttpHandler(_ => throw new HttpRequestException("down")));

            var result = await gateway.RequestAsync(250_000, "desc", "https://cb");

            Assert.False(result.Success);
            Assert.Equal("NETWORK", result.ErrorCode);
        }

        [Fact]
        public async Task Request_timeout_returns_network_error()
        {
            var gateway = CreateGateway(new StubHttpHandler(_ => throw new TaskCanceledException("timeout")));

            var result = await gateway.RequestAsync(250_000, "desc", "https://cb");

            Assert.False(result.Success);
            Assert.Equal("NETWORK", result.ErrorCode);
        }

        [Fact]
        public async Task Request_caller_cancellation_is_not_swallowed()
        {
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();
            var gateway = CreateGateway(new StubHttpHandler(_ => Json(RequestSuccessJson)));

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                gateway.RequestAsync(250_000, "desc", "https://cb", cts.Token));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Request_without_merchant_id_fails_without_calling_the_network(string merchantId)
        {
            var handler = new StubHttpHandler(_ => Json(RequestSuccessJson));

            var result = await CreateGateway(handler, merchantId).RequestAsync(250_000, "desc", "https://cb");

            Assert.False(result.Success);
            Assert.Equal("NOT_CONFIGURED", result.ErrorCode);
            Assert.Empty(handler.Requests);
        }

        [Fact]
        public async Task Verify_paid_returns_ref_id_and_card_details()
        {
            var gateway = CreateGateway(new StubHttpHandler(_ => Json(VerifyPaidJson)));

            var result = await gateway.VerifyAsync(250_000, "A0001");

            Assert.True(result.Success);
            Assert.Equal(201, result.RefId);
            Assert.Equal("603799******1234", result.CardPan);
            Assert.Equal("HASH", result.CardHash);
            Assert.Null(result.ErrorCode);
        }

        [Fact]
        public async Task Verify_code_101_already_verified_counts_as_success()
        {
            var gateway = CreateGateway(new StubHttpHandler(_ => Json(VerifyAlreadyJson)));

            var result = await gateway.VerifyAsync(250_000, "A0001");

            Assert.True(result.Success);
            Assert.Equal(201, result.RefId);
        }

        [Fact]
        public async Task Verify_posts_expected_body_to_verify_endpoint()
        {
            var handler = new StubHttpHandler(_ => Json(VerifyPaidJson));

            await CreateGateway(handler).VerifyAsync(250_000, "A0001");

            var call = Assert.Single(handler.Requests);
            Assert.Equal(HttpMethod.Post, call.Method);
            Assert.Equal($"{BaseUrl}/pg/v4/payment/verify.json", call.Uri!.ToString());

            using var doc = JsonDocument.Parse(call.Body);
            var root = doc.RootElement;
            Assert.Equal(MerchantId, root.GetProperty("merchant_id").GetString());
            Assert.Equal(250_000, root.GetProperty("amount").GetInt64());
            Assert.Equal("A0001", root.GetProperty("authority").GetString());
        }

        [Fact]
        public async Task Verify_not_paid_returns_failure_with_gateway_error()
        {
            var gateway = CreateGateway(new StubHttpHandler(_ => Json(VerifyNotPaidJson)));

            var result = await gateway.VerifyAsync(250_000, "A0001");

            Assert.False(result.Success);
            Assert.Null(result.RefId);
            Assert.Equal("-51", result.ErrorCode);
            Assert.Contains("not active", result.ErrorMessage);
        }

        [Fact]
        public async Task Verify_html_error_page_throws_so_the_payment_stays_retryable()
        {
            var gateway = CreateGateway(new StubHttpHandler(_ =>
                new HttpResponseMessage(HttpStatusCode.BadGateway)
                {
                    Content = new StringContent("<html>Bad Gateway</html>", Encoding.UTF8, "text/html"),
                }));

            var ex = await Assert.ThrowsAsync<HttpRequestException>(() => gateway.VerifyAsync(250_000, "A0001"));

            Assert.Equal(HttpStatusCode.BadGateway, ex.StatusCode);
        }

        [Theory]
        [InlineData("")]
        [InlineData("not json")]
        [InlineData("{}")]
        [InlineData("""{"data":[],"errors":[]}""")]
        public async Task Verify_response_without_data_or_error_object_throws(string body)
        {
            var gateway = CreateGateway(new StubHttpHandler(_ => Json(body)));

            await Assert.ThrowsAsync<HttpRequestException>(() => gateway.VerifyAsync(250_000, "A0001"));
        }

        [Fact]
        public async Task Verify_5xx_with_json_gateway_error_is_still_reported_as_gateway_failure()
        {
            var gateway = CreateGateway(new StubHttpHandler(_ => Json(VerifyNotPaidJson, HttpStatusCode.InternalServerError)));

            var result = await gateway.VerifyAsync(250_000, "A0001");

            Assert.False(result.Success);
            Assert.Equal("-51", result.ErrorCode);
        }

        [Fact]
        public async Task Verify_code_other_than_100_or_101_with_data_object_is_failure()
        {
            var gateway = CreateGateway(new StubHttpHandler(_ =>
                Json("""{"data":{"code":102,"message":"x"},"errors":[]}""")));

            var result = await gateway.VerifyAsync(250_000, "A0001");

            Assert.False(result.Success);
            Assert.Null(result.RefId);
        }

        [Fact]
        public async Task Verify_connection_failure_is_not_swallowed()
        {
            var gateway = CreateGateway(new StubHttpHandler(_ => throw new HttpRequestException("down")));

            await Assert.ThrowsAsync<HttpRequestException>(() => gateway.VerifyAsync(250_000, "A0001"));
        }

        [Fact]
        public async Task Verify_without_merchant_id_throws_and_does_not_call_the_network()
        {
            var handler = new StubHttpHandler(_ => Json(VerifyPaidJson));

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                CreateGateway(handler, merchantId: "").VerifyAsync(250_000, "A0001"));

            Assert.Empty(handler.Requests);
        }
    }

    internal sealed class StubHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public List<(HttpMethod Method, Uri? Uri, string Body)> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add((request.Method, request.RequestUri, body));
            return respond(request);
        }
    }
}
