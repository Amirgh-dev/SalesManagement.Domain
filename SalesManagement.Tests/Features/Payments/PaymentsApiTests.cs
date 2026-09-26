using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SalesManagement.Domain.Enums;
using SalesManagement.Domain.Features.Invoices;
using SalesManagement.Domain.Features.Payments;

namespace SalesManagement.Tests.Features.Payments
{
    [Collection(PaymentsApiCollection.Name)]
    public class PaymentsApiTests
    {
        private const int PartyId = 7823;
        private const int OtherPartyId = 9001;
        private const int InvoiceId = 4100;
        private const int InvoiceNumber = 3833;
        private const long NetPrice = 248_200_000;
        private const string ResultPage = TestSettings.FrontendResultUrl + "/payment-result";

        private readonly PaymentsApiFactory _factory;

        public PaymentsApiTests(PaymentsApiFactory factory)
        {
            _factory = factory;
            _factory.Reset();
        }

        private static StringContent Body(object payload) =>
            new(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        private static StringContent RawJson(string json) => new(json, Encoding.UTF8, "application/json");

        private static async Task<JsonElement> ReadJson(HttpResponseMessage response)
        {
            var text = await response.Content.ReadAsStringAsync();
            return JsonDocument.Parse(text).RootElement.Clone();
        }

        private static async Task<string?> Detail(HttpResponseMessage response)
        {
            var json = await ReadJson(response);
            return json.TryGetProperty("detail", out var detail) ? detail.GetString() : null;
        }

        private static string Location(HttpResponseMessage response) =>
            response.Headers.Location!.OriginalString;

        private static string CallbackUrl(Guid paymentId, string status, string? authority) =>
            $"/api/payments/callback?paymentId={paymentId}&Status={status}"
            + (authority is null ? "" : $"&Authority={authority}");

        private Payment Stored(Guid id) => _factory.Payments.All.Single(p => p.Id == id);

        private void AddInvoice(int invoiceId = InvoiceId, int number = InvoiceNumber, long netPrice = NetPrice,
            int owner = PartyId) =>
            _factory.Invoices.Add(new PayableInvoice(invoiceId, number, netPrice), owner);

        private async Task<(Guid PaymentId, string Authority)> RequestPaymentAsync(
            int partyId = PartyId, long amount = 250_000)
        {
            using var client = _factory.CreateApiClient(partyId);
            var response = await client.PostAsync(
                "/api/payments/request", Body(new { amount, description = "test", channel = "web" }));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var id = (await ReadJson(response)).GetProperty("paymentId").GetGuid();
            return (id, Stored(id).Authority!);
        }

        private async Task<(Guid PaymentId, string Authority)> RequestInvoicePaymentAsync(
            int invoiceId = InvoiceId, int partyId = PartyId)
        {
            using var client = _factory.CreateApiClient(partyId);
            var response = await client.PostAsync("/api/payments/invoice", Body(new { invoiceId, channel = "web" }));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var id = (await ReadJson(response)).GetProperty("paymentId").GetGuid();
            return (id, Stored(id).Authority!);
        }

        private async Task<HttpResponseMessage> CallbackAsync(Guid paymentId, string status, string? authority)
        {
            using var client = _factory.CreateApiClient();
            return await client.GetAsync(CallbackUrl(paymentId, status, authority));
        }

        [Fact]
        public async Task Request_without_token_is_401_with_login_message()
        {
            using var client = _factory.CreateApiClient();

            var response = await client.PostAsync(
                "/api/payments/request", Body(new { amount = 1000, description = "d", channel = "web" }));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal("برای این عملیات ابتدا باید وارد شوید", await Detail(response));
            Assert.Empty(_factory.Payments.All);
        }

        [Fact]
        public async Task Request_with_garbage_token_is_401()
        {
            using var client = _factory.CreateApiClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "garbage");

            var response = await client.PostAsync(
                "/api/payments/request", Body(new { amount = 1000, description = "d", channel = "web" }));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal("توکن نامعتبر است", await Detail(response));
        }

        [Fact]
        public async Task Invoice_without_token_is_401()
        {
            using var client = _factory.CreateApiClient();

            var response = await client.PostAsync("/api/payments/invoice", Body(new { invoiceId = 1, channel = "web" }));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Callback_is_reachable_without_token()
        {
            using var client = _factory.CreateApiClient();

            var response = await client.GetAsync(CallbackUrl(Guid.NewGuid(), "OK", "x"));

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        }

        [Theory]
        [InlineData("GET", "/api/payments/request")]
        [InlineData("GET", "/api/payments/invoice")]
        [InlineData("POST", "/api/payments/callback")]
        [InlineData("PUT", "/api/payments/request")]
        [InlineData("DELETE", "/api/payments/invoice")]
        public async Task Wrong_http_method_is_rejected(string method, string url)
        {
            using var client = _factory.CreateApiClient(PartyId);

            var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), url));

            Assert.Contains(response.StatusCode, new[] { HttpStatusCode.MethodNotAllowed, HttpStatusCode.NotFound });
            Assert.Empty(_factory.Payments.All);
        }

        [Fact]
        public async Task Request_returns_payment_id_and_gateway_url()
        {
            using var client = _factory.CreateApiClient(PartyId);

            var response = await client.PostAsync(
                "/api/payments/request", Body(new { amount = 250_000, description = "شارژ", channel = "web" }));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var json = await ReadJson(response);
            Assert.Equal($"{TestSettings.ZarinPalBaseUrl}/pg/StartPay/AUTH-STUB-1", json.GetProperty("paymentUrl").GetString());
            Assert.NotEqual(Guid.Empty, json.GetProperty("paymentId").GetGuid());
        }

        [Fact]
        public async Task Request_stores_a_sent_to_gateway_payment_for_the_token_owner()
        {
            var (id, _) = await RequestPaymentAsync(PartyId, 250_000);

            var stored = Stored(id);
            Assert.Equal(PartyId, stored.PartyRef);
            Assert.Equal(PaymentState.SentToGateway, stored.State);
            Assert.Equal("AUTH-STUB-1", stored.Authority);
            Assert.Equal(1000, stored.Fee);
            Assert.Equal("ZarinPal", stored.Gateway);
            Assert.Equal(250_000, stored.Amount);
            Assert.Equal("web", stored.Channel);
            Assert.False(stored.IsPay);
            Assert.False(stored.IsOk);
            Assert.False(string.IsNullOrWhiteSpace(stored.IpAddress));
            Assert.Equal(15, stored.TrackingCode!.Length);
        }

        [Fact]
        public async Task Request_ignores_party_table_and_target_sent_in_the_body()
        {
            using var client = _factory.CreateApiClient(PartyId);

            var response = await client.PostAsync(
                "/api/payments/request",
                RawJson("""{"amount":1000,"description":"d","channel":"web","partyRef":1,"tableName":"Invoice","targetRef":49946}"""));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var stored = Assert.Single(_factory.Payments.All);
            Assert.Equal(PartyId, stored.PartyRef);
            Assert.Null(stored.TableName);
            Assert.Null(stored.TargetRef);
        }

        [Fact]
        public async Task Different_tokens_produce_payments_for_different_parties()
        {
            await RequestPaymentAsync(PartyId);
            await RequestPaymentAsync(OtherPartyId);

            Assert.Equal(new[] { PartyId, OtherPartyId }, _factory.Payments.All.Select(p => p.PartyRef).OrderByDescending(x => x == PartyId).ToArray());
        }

        [Fact]
        public async Task Request_sends_merchant_amount_description_and_callback_to_zarinpal()
        {
            var (id, _) = await RequestPaymentAsync(PartyId, 777_000);

            var call = Assert.Single(_factory.ZarinPal.Calls);
            Assert.EndsWith("/pg/v4/payment/request.json", call.Path);
            using var doc = JsonDocument.Parse(call.Body);
            var root = doc.RootElement;
            Assert.Equal(TestSettings.MerchantId, root.GetProperty("merchant_id").GetString());
            Assert.Equal(777_000, root.GetProperty("amount").GetInt64());
            Assert.Equal("test", root.GetProperty("description").GetString());
            Assert.Equal(
                $"{TestSettings.CallbackBaseUrl}/api/payments/callback?paymentId={id}",
                root.GetProperty("callback_url").GetString());
        }

        [Theory]
        [InlineData("missing-amount")]
        [InlineData("zero-amount")]
        [InlineData("negative-amount")]
        [InlineData("string-amount")]
        [InlineData("missing-description")]
        [InlineData("empty-description")]
        [InlineData("long-description")]
        [InlineData("missing-channel")]
        [InlineData("empty-channel")]
        [InlineData("long-channel")]
        [InlineData("empty-object")]
        public async Task Request_with_invalid_body_is_400_and_nothing_happens(string scenario)
        {
            var payload = scenario switch
            {
                "missing-amount" => """{"description":"d","channel":"web"}""",
                "zero-amount" => """{"amount":0,"description":"d","channel":"web"}""",
                "negative-amount" => """{"amount":-5,"description":"d","channel":"web"}""",
                "string-amount" => """{"amount":"abc","description":"d","channel":"web"}""",
                "missing-description" => """{"amount":1000,"channel":"web"}""",
                "empty-description" => """{"amount":1000,"description":"","channel":"web"}""",
                "long-description" => $$"""{"amount":1000,"description":"{{new string('d', 501)}}","channel":"web"}""",
                "missing-channel" => """{"amount":1000,"description":"d"}""",
                "empty-channel" => """{"amount":1000,"description":"d","channel":""}""",
                "long-channel" => $$"""{"amount":1000,"description":"d","channel":"{{new string('c', 31)}}"}""",
                _ => "{}",
            };
            using var client = _factory.CreateApiClient(PartyId);

            var response = await client.PostAsync("/api/payments/request", RawJson(payload));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Empty(_factory.Payments.All);
            Assert.Empty(_factory.ZarinPal.Calls);
        }

        [Fact]
        public async Task Request_with_malformed_json_is_400()
        {
            using var client = _factory.CreateApiClient(PartyId);

            var response = await client.PostAsync("/api/payments/request", RawJson("{ not json"));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Empty(_factory.Payments.All);
        }

        [Fact]
        public async Task Request_with_empty_body_is_400()
        {
            using var client = _factory.CreateApiClient(PartyId);

            var response = await client.PostAsync("/api/payments/request", RawJson(""));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Request_with_plain_text_content_type_is_415()
        {
            using var client = _factory.CreateApiClient(PartyId);

            var response = await client.PostAsync(
                "/api/payments/request", new StringContent("amount=1000", Encoding.UTF8, "text/plain"));

            Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
        }

        [Fact]
        public async Task Request_when_gateway_rejects_is_400_and_payment_is_request_failed()
        {
            _factory.ZarinPal.OnRequest = () => ZarinPalStub.Json(ZarinPalStub.ErrorJson);
            using var client = _factory.CreateApiClient(PartyId);

            var response = await client.PostAsync(
                "/api/payments/request", Body(new { amount = 1000, description = "d", channel = "web" }));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("امکان اتصال به درگاه پرداخت وجود ندارد", await Detail(response));
            var stored = Assert.Single(_factory.Payments.All);
            Assert.Equal(PaymentState.RequestFailed, stored.State);
            Assert.Equal("-9", stored.ErrorCode);
            Assert.Equal("Validation error", stored.ErrorMessage);
            Assert.Null(stored.Authority);
        }

        [Fact]
        public async Task Request_when_gateway_returns_html_error_page_is_400_and_request_failed()
        {
            _factory.ZarinPal.OnRequest = () =>
                new HttpResponseMessage(HttpStatusCode.BadGateway)
                {
                    Content = new StringContent("<html>502</html>", Encoding.UTF8, "text/html"),
                };
            using var client = _factory.CreateApiClient(PartyId);

            var response = await client.PostAsync(
                "/api/payments/request", Body(new { amount = 1000, description = "d", channel = "web" }));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(PaymentState.RequestFailed, Assert.Single(_factory.Payments.All).State);
        }

        [Fact]
        public async Task Request_when_gateway_is_unreachable_is_400_with_network_error_recorded()
        {
            _factory.ZarinPal.OnRequest = () => throw new HttpRequestException("down");
            using var client = _factory.CreateApiClient(PartyId);

            var response = await client.PostAsync(
                "/api/payments/request", Body(new { amount = 1000, description = "d", channel = "web" }));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var stored = Assert.Single(_factory.Payments.All);
            Assert.Equal(PaymentState.RequestFailed, stored.State);
            Assert.Equal("NETWORK", stored.ErrorCode);
        }

        [Fact]
        public async Task Request_when_gateway_success_has_no_authority_is_request_failed()
        {
            _factory.ZarinPal.OnRequest = () =>
                ZarinPalStub.Json("""{"data":{"code":100,"message":"Success"},"errors":[]}""");
            using var client = _factory.CreateApiClient(PartyId);

            var response = await client.PostAsync(
                "/api/payments/request", Body(new { amount = 1000, description = "d", channel = "web" }));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(PaymentState.RequestFailed, Assert.Single(_factory.Payments.All).State);
        }

        [Fact]
        public async Task Each_request_creates_a_new_payment_with_its_own_authority_and_tracking_code()
        {
            var first = await RequestPaymentAsync();
            var second = await RequestPaymentAsync();

            Assert.NotEqual(first.PaymentId, second.PaymentId);
            Assert.NotEqual(first.Authority, second.Authority);
            Assert.Equal(2, _factory.Payments.All.Count);
        }

        [Fact]
        public async Task Invoice_payment_uses_the_invoice_net_price_and_tags_the_record()
        {
            AddInvoice();

            var (id, _) = await RequestInvoicePaymentAsync();

            var stored = Stored(id);
            Assert.Equal(NetPrice, stored.Amount);
            Assert.Equal("Invoice", stored.TableName);
            Assert.Equal(InvoiceId, stored.TargetRef);
            Assert.Equal(PartyId, stored.PartyRef);
            Assert.Contains(InvoiceNumber.ToString(), stored.Description);
            Assert.Equal(PaymentState.SentToGateway, stored.State);
        }

        [Fact]
        public async Task Invoice_payment_sends_the_invoice_amount_to_zarinpal()
        {
            AddInvoice();

            await RequestInvoicePaymentAsync();

            using var doc = JsonDocument.Parse(Assert.Single(_factory.ZarinPal.Calls).Body);
            Assert.Equal(NetPrice, doc.RootElement.GetProperty("amount").GetInt64());
        }

        [Fact]
        public async Task Invoice_payment_ignores_an_amount_sent_by_the_client()
        {
            AddInvoice();
            using var client = _factory.CreateApiClient(PartyId);

            var response = await client.PostAsync(
                "/api/payments/invoice", RawJson($$"""{"invoiceId":{{InvoiceId}},"channel":"web","amount":1}"""));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(NetPrice, Assert.Single(_factory.Payments.All).Amount);
        }

        [Fact]
        public async Task Invoice_payment_supports_amounts_above_int_range()
        {
            AddInvoice(netPrice: 1_445_760_000);

            await RequestInvoicePaymentAsync();

            using var doc = JsonDocument.Parse(Assert.Single(_factory.ZarinPal.Calls).Body);
            Assert.Equal(1_445_760_000, doc.RootElement.GetProperty("amount").GetInt64());
        }

        [Fact]
        public async Task Invoice_of_another_customer_is_400_not_found_and_gateway_is_not_called()
        {
            AddInvoice(owner: OtherPartyId);
            using var client = _factory.CreateApiClient(PartyId);

            var response = await client.PostAsync("/api/payments/invoice", Body(new { invoiceId = InvoiceId, channel = "web" }));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("فاکتور پیدا نشد", await Detail(response));
            Assert.Empty(_factory.ZarinPal.Calls);
            Assert.Empty(_factory.Payments.All);
        }

        [Fact]
        public async Task Unknown_invoice_gives_the_same_not_found_message_as_someone_elses_invoice()
        {
            using var client = _factory.CreateApiClient(PartyId);

            var response = await client.PostAsync("/api/payments/invoice", Body(new { invoiceId = 999999, channel = "web" }));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("فاکتور پیدا نشد", await Detail(response));
        }

        [Fact]
        public async Task Invoice_with_zero_net_price_is_rejected()
        {
            AddInvoice(netPrice: 0);
            using var client = _factory.CreateApiClient(PartyId);

            var response = await client.PostAsync("/api/payments/invoice", Body(new { invoiceId = InvoiceId, channel = "web" }));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("مبلغ فاکتور نامعتبر است", await Detail(response));
            Assert.Empty(_factory.ZarinPal.Calls);
        }

        [Theory]
        [InlineData("""{"channel":"web"}""")]
        [InlineData("""{"invoiceId":0,"channel":"web"}""")]
        [InlineData("""{"invoiceId":-5,"channel":"web"}""")]
        [InlineData("""{"invoiceId":"abc","channel":"web"}""")]
        [InlineData("""{"invoiceId":1}""")]
        [InlineData("""{"invoiceId":1,"channel":""}""")]
        [InlineData("{}")]
        public async Task Invoice_with_invalid_body_is_400_and_nothing_happens(string payload)
        {
            using var client = _factory.CreateApiClient(PartyId);

            var response = await client.PostAsync("/api/payments/invoice", RawJson(payload));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Empty(_factory.Payments.All);
            Assert.Empty(_factory.ZarinPal.Calls);
        }

        [Fact]
        public async Task Paying_the_same_invoice_again_after_success_is_rejected()
        {
            AddInvoice();
            var (id, authority) = await RequestInvoicePaymentAsync();
            await CallbackAsync(id, "OK", authority);
            using var client = _factory.CreateApiClient(PartyId);

            var response = await client.PostAsync("/api/payments/invoice", Body(new { invoiceId = InvoiceId, channel = "web" }));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("این فاکتور قبلاً پرداخت شده است", await Detail(response));
            Assert.Equal(1, _factory.ZarinPal.RequestCalls);
        }

        [Fact]
        public async Task A_second_invoice_can_still_be_paid_after_the_first_succeeded()
        {
            AddInvoice(invoiceId: 1, number: 11);
            AddInvoice(invoiceId: 2, number: 22);
            var (id, authority) = await RequestInvoicePaymentAsync(invoiceId: 1);
            await CallbackAsync(id, "OK", authority);

            var (secondId, _) = await RequestInvoicePaymentAsync(invoiceId: 2);

            Assert.Equal(2, Stored(secondId).TargetRef);
        }

        [Theory]
        [InlineData("cancel")]
        [InlineData("not-paid")]
        [InlineData("abandoned")]
        public async Task Invoice_can_be_retried_after_a_cancelled_failed_or_abandoned_attempt(string previous)
        {
            AddInvoice();
            var (id, authority) = await RequestInvoicePaymentAsync();
            switch (previous)
            {
                case "cancel":
                    await CallbackAsync(id, "NOK", authority);
                    break;
                case "not-paid":
                    _factory.ZarinPal.OnVerify = () => ZarinPalStub.Json(ZarinPalStub.VerifyNotPaidJson);
                    await CallbackAsync(id, "OK", authority);
                    break;
            }

            var (retryId, _) = await RequestInvoicePaymentAsync();

            Assert.NotEqual(id, retryId);
            Assert.Equal(PaymentState.SentToGateway, Stored(retryId).State);
        }

        [Fact]
        public async Task Another_customers_verified_payment_for_the_same_invoice_id_does_not_block()
        {
            AddInvoice();
            _factory.Payments.Seed(
                Payment.Create(OtherPartyId, "Invoice", InvoiceId, "ZarinPal", 10_000, "x", "web", "1.1.1.1")
                    .MarkSentToGateway("A", null)
                    .MarkVerified(1, null, null));

            var (id, _) = await RequestInvoicePaymentAsync();

            Assert.Equal(PaymentState.SentToGateway, Stored(id).State);
        }

        [Fact]
        public async Task Callback_success_redirects_to_frontend_and_marks_payment_paid()
        {
            var (id, authority) = await RequestPaymentAsync();

            var response = await CallbackAsync(id, "OK", authority);

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            var stored = Stored(id);
            Assert.Equal($"{ResultPage}?success=true&trackingCode={stored.TrackingCode}", Location(response));
            Assert.Equal(PaymentState.Verified, stored.State);
            Assert.True(stored.IsPay);
            Assert.False(stored.IsOk);
            Assert.Equal(900001, stored.RefId);
            Assert.Equal("603799******1234", stored.CardPan);
            Assert.Equal("HASH", stored.CardHash);
            Assert.NotNull(stored.VerifiedDate);
        }

        [Fact]
        public async Task Callback_verifies_with_the_stored_amount_and_authority()
        {
            AddInvoice();
            var (id, authority) = await RequestInvoicePaymentAsync();

            await CallbackAsync(id, "OK", authority);

            var verify = Assert.Single(_factory.ZarinPal.Calls, c => c.Path.EndsWith("/verify.json"));
            using var doc = JsonDocument.Parse(verify.Body);
            Assert.Equal(TestSettings.MerchantId, doc.RootElement.GetProperty("merchant_id").GetString());
            Assert.Equal(NetPrice, doc.RootElement.GetProperty("amount").GetInt64());
            Assert.Equal(authority, doc.RootElement.GetProperty("authority").GetString());
        }

        [Fact]
        public async Task Callback_cancel_redirects_with_failure_and_does_not_call_verify()
        {
            var (id, authority) = await RequestPaymentAsync();

            var response = await CallbackAsync(id, "NOK", authority);

            Assert.Equal($"{ResultPage}?success=false&trackingCode={Stored(id).TrackingCode}", Location(response));
            Assert.Equal(PaymentState.CanceledByUser, Stored(id).State);
            Assert.False(Stored(id).IsPay);
            Assert.Equal(0, _factory.ZarinPal.VerifyCalls);
        }

        [Fact]
        public async Task Callback_with_wrong_authority_redirects_to_failure_without_tracking_and_leaves_payment_untouched()
        {
            var (id, _) = await RequestPaymentAsync();

            var response = await CallbackAsync(id, "OK", "WRONG");

            Assert.Equal($"{ResultPage}?success=false", Location(response));
            Assert.Equal(PaymentState.SentToGateway, Stored(id).State);
            Assert.Equal(0, _factory.ZarinPal.VerifyCalls);
        }

        [Fact]
        public async Task Callback_without_authority_is_treated_as_mismatch()
        {
            var (id, _) = await RequestPaymentAsync();

            var response = await CallbackAsync(id, "OK", null);

            Assert.Equal($"{ResultPage}?success=false", Location(response));
            Assert.Equal(PaymentState.SentToGateway, Stored(id).State);
        }

        [Fact]
        public async Task Callback_with_authority_of_another_payment_is_rejected()
        {
            var first = await RequestPaymentAsync();
            var second = await RequestPaymentAsync();

            var response = await CallbackAsync(first.PaymentId, "OK", second.Authority);

            Assert.Equal($"{ResultPage}?success=false", Location(response));
            Assert.Equal(PaymentState.SentToGateway, Stored(first.PaymentId).State);
            Assert.Equal(PaymentState.SentToGateway, Stored(second.PaymentId).State);
            Assert.Equal(0, _factory.ZarinPal.VerifyCalls);
        }

        [Fact]
        public async Task Callback_for_unknown_payment_redirects_to_failure()
        {
            var response = await CallbackAsync(Guid.NewGuid(), "OK", "A");

            Assert.Equal($"{ResultPage}?success=false", Location(response));
            Assert.Equal(0, _factory.ZarinPal.VerifyCalls);
        }

        [Fact]
        public async Task Callback_without_payment_id_redirects_to_failure()
        {
            using var client = _factory.CreateApiClient();

            var response = await client.GetAsync("/api/payments/callback?Status=OK&Authority=A");

            Assert.Equal($"{ResultPage}?success=false", Location(response));
        }

        [Fact]
        public async Task Callback_with_malformed_payment_id_is_400()
        {
            using var client = _factory.CreateApiClient();

            var response = await client.GetAsync("/api/payments/callback?paymentId=abc&Status=OK&Authority=A");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Callback_when_gateway_says_not_paid_marks_verify_failed()
        {
            var (id, authority) = await RequestPaymentAsync();
            _factory.ZarinPal.OnVerify = () => ZarinPalStub.Json(ZarinPalStub.VerifyNotPaidJson);

            var response = await CallbackAsync(id, "OK", authority);

            var stored = Stored(id);
            Assert.Equal($"{ResultPage}?success=false&trackingCode={stored.TrackingCode}", Location(response));
            Assert.Equal(PaymentState.VerifyFailed, stored.State);
            Assert.False(stored.IsPay);
            Assert.Equal("-51", stored.ErrorCode);
        }

        [Fact]
        public async Task Callback_repeated_after_success_is_idempotent_and_verifies_only_once()
        {
            var (id, authority) = await RequestPaymentAsync();

            var first = await CallbackAsync(id, "OK", authority);
            var second = await CallbackAsync(id, "OK", authority);

            Assert.Equal(Location(first), Location(second));
            Assert.Equal(1, _factory.ZarinPal.VerifyCalls);
            Assert.Equal(PaymentState.Verified, Stored(id).State);
        }

        [Fact]
        public async Task Cancel_callback_after_success_cannot_undo_the_payment()
        {
            var (id, authority) = await RequestPaymentAsync();
            await CallbackAsync(id, "OK", authority);

            var response = await CallbackAsync(id, "NOK", authority);

            Assert.Contains("success=true", Location(response));
            Assert.Equal(PaymentState.Verified, Stored(id).State);
            Assert.True(Stored(id).IsPay);
        }

        [Fact]
        public async Task Success_callback_after_cancel_cannot_revive_the_payment()
        {
            var (id, authority) = await RequestPaymentAsync();
            await CallbackAsync(id, "NOK", authority);

            var response = await CallbackAsync(id, "OK", authority);

            Assert.Contains("success=false", Location(response));
            Assert.Equal(PaymentState.CanceledByUser, Stored(id).State);
            Assert.Equal(0, _factory.ZarinPal.VerifyCalls);
        }

        [Fact]
        public async Task Callback_after_verify_failed_does_not_query_the_gateway_again()
        {
            var (id, authority) = await RequestPaymentAsync();
            _factory.ZarinPal.OnVerify = () => ZarinPalStub.Json(ZarinPalStub.VerifyNotPaidJson);
            await CallbackAsync(id, "OK", authority);
            _factory.ZarinPal.OnVerify = null;

            var response = await CallbackAsync(id, "OK", authority);

            Assert.Contains("success=false", Location(response));
            Assert.Equal(1, _factory.ZarinPal.VerifyCalls);
            Assert.Equal(PaymentState.VerifyFailed, Stored(id).State);
        }

        [Fact]
        public async Task Callback_query_parameter_names_are_case_insensitive()
        {
            var (id, authority) = await RequestPaymentAsync();
            using var client = _factory.CreateApiClient();

            var response = await client.GetAsync($"/api/payments/callback?PAYMENTID={id}&status=ok&authority={authority}");

            Assert.Contains("success=true", Location(response));
        }

        [Fact]
        public async Task Callback_without_status_is_treated_as_cancel()
        {
            var (id, authority) = await RequestPaymentAsync();
            using var client = _factory.CreateApiClient();

            var response = await client.GetAsync($"/api/payments/callback?paymentId={id}&Authority={authority}");

            Assert.Contains("success=false", Location(response));
            Assert.Equal(PaymentState.CanceledByUser, Stored(id).State);
            Assert.Equal(0, _factory.ZarinPal.VerifyCalls);
        }

        [Fact]
        public async Task Callback_for_a_payment_that_never_reached_the_gateway_is_rejected()
        {
            var created = _factory.Payments.Seed(
                Payment.Create(PartyId, null, null, "ZarinPal", 1000, "d", "web", "1.1.1.1"));

            var response = await CallbackAsync(created.Id, "OK", "anything");

            Assert.Equal($"{ResultPage}?success=false", Location(response));
            Assert.Equal(PaymentState.Created, Stored(created.Id).State);
            Assert.Equal(0, _factory.ZarinPal.VerifyCalls);
        }

        [Fact]
        public async Task Callback_for_a_request_failed_payment_reports_failure_with_its_tracking_code()
        {
            var failed = _factory.Payments.Seed(
                Payment.Create(PartyId, null, null, "ZarinPal", 1000, "d", "web", "1.1.1.1")
                    .MarkRequestFailed("-9", "x"));

            var response = await CallbackAsync(failed.Id, "OK", "anything");

            Assert.Equal($"{ResultPage}?success=false&trackingCode={failed.TrackingCode}", Location(response));
            Assert.Equal(PaymentState.RequestFailed, Stored(failed.Id).State);
            Assert.Equal(0, _factory.ZarinPal.VerifyCalls);
        }

        [Fact]
        public async Task Callback_when_gateway_is_down_returns_500_and_keeps_the_payment_retryable()
        {
            var (id, authority) = await RequestPaymentAsync();
            _factory.ZarinPal.OnVerify = () => throw new HttpRequestException("down");

            var down = await CallbackAsync(id, "OK", authority);

            Assert.Equal(HttpStatusCode.InternalServerError, down.StatusCode);
            Assert.Equal(PaymentState.SentToGateway, Stored(id).State);
            Assert.False(Stored(id).IsPay);

            _factory.ZarinPal.OnVerify = null;
            var retry = await CallbackAsync(id, "OK", authority);

            Assert.Contains("success=true", Location(retry));
            Assert.Equal(PaymentState.Verified, Stored(id).State);
        }

        [Fact]
        public async Task Callback_when_gateway_returns_html_error_keeps_the_payment_retryable()
        {
            var (id, authority) = await RequestPaymentAsync();
            _factory.ZarinPal.OnVerify = () =>
                new HttpResponseMessage(HttpStatusCode.BadGateway)
                {
                    Content = new StringContent("<html>502</html>", Encoding.UTF8, "text/html"),
                };

            var broken = await CallbackAsync(id, "OK", authority);

            Assert.Equal(HttpStatusCode.InternalServerError, broken.StatusCode);
            Assert.Equal(PaymentState.SentToGateway, Stored(id).State);

            _factory.ZarinPal.OnVerify = null;
            var retry = await CallbackAsync(id, "OK", authority);

            Assert.Contains("success=true", Location(retry));
        }

        [Fact]
        public async Task Callback_when_gateway_confirms_with_code_101_is_success()
        {
            var (id, authority) = await RequestPaymentAsync();
            _factory.ZarinPal.OnVerify = () => ZarinPalStub.Json(
                """{"data":{"code":101,"message":"Verified","card_pan":"603799******1234","card_hash":"H","ref_id":900002},"errors":[]}""");

            var response = await CallbackAsync(id, "OK", authority);

            Assert.Contains("success=true", Location(response));
            Assert.Equal(900002, Stored(id).RefId);
        }

        [Fact]
        public async Task Callback_when_gateway_confirms_without_ref_id_is_failure()
        {
            var (id, authority) = await RequestPaymentAsync();
            _factory.ZarinPal.OnVerify = () => ZarinPalStub.Json(
                """{"data":{"code":100,"message":"Paid","card_pan":"x","card_hash":"y"},"errors":[]}""");

            var response = await CallbackAsync(id, "OK", authority);

            Assert.Contains("success=false", Location(response));
            Assert.Equal(PaymentState.VerifyFailed, Stored(id).State);
            Assert.False(Stored(id).IsPay);
        }

        [Fact]
        public async Task Full_invoice_flow_end_to_end()
        {
            AddInvoice();

            var (id, authority) = await RequestInvoicePaymentAsync();
            var callback = await CallbackAsync(id, "OK", authority);

            var stored = Stored(id);
            Assert.Equal($"{ResultPage}?success=true&trackingCode={stored.TrackingCode}", Location(callback));
            Assert.Equal(PaymentState.Verified, stored.State);
            Assert.Equal(NetPrice, stored.Amount);
            Assert.True(stored.IsPay);
            Assert.False(stored.IsOk);
            Assert.True(await _factory.Payments.ExistsVerifiedAsync(PartyId, "Invoice", InvoiceId));
        }
    }
}
