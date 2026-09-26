using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using SalesManagement.Application.Common.Interfaces;

namespace SalesManagement.Infrastructure.Payments
{
    public class ZarinPalGateway(HttpClient httpClient, IOptions<ZarinPalSettings> settings) : IPaymentGateway
    {
        private readonly ZarinPalSettings _settings = settings.Value;

        public string Name => "ZarinPal";

        public async Task<GatewayRequestResult> RequestAsync(
            long amount, string description, string callbackUrl, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_settings.MerchantId))
            {
                return new GatewayRequestResult(
                    false, null, null, null, "NOT_CONFIGURED", "درگاه پرداخت پیکربندی نشده است");
            }

            var body = new ZarinPalRequestBody(_settings.MerchantId, amount, description, callbackUrl);

            ZarinPalResponse? payload;
            try
            {
                var response = await httpClient.PostAsJsonAsync(
                    $"{_settings.BaseUrl}/pg/v4/payment/request.json", body, cancellationToken);
                payload = await ReadAsync(response, cancellationToken);
            }
            catch (Exception ex) when (ex is HttpRequestException
                                       || (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested))
            {
                return new GatewayRequestResult(false, null, null, null, "NETWORK", "ارتباط با درگاه پرداخت برقرار نشد");
            }

            var data = ReadObject<ZarinPalRequestData>(payload?.Data);

            if (data?.Code is 100 && data.Authority is not null)
            {
                var paymentUrl = $"{_settings.BaseUrl}/pg/StartPay/{data.Authority}";
                return new GatewayRequestResult(true, data.Authority, paymentUrl, data.Fee, null, null);
            }

            var error = ReadObject<ZarinPalError>(payload?.Errors);
            return new GatewayRequestResult(false, null, null, null, error?.Code?.ToString(), error?.Message);
        }

        public async Task<GatewayVerifyResult> VerifyAsync(
            long amount, string authority, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_settings.MerchantId))
                throw new InvalidOperationException("ZarinPal MerchantId is not configured.");

            var body = new ZarinPalVerifyBody(_settings.MerchantId, amount, authority);

            var response = await httpClient.PostAsJsonAsync(
                $"{_settings.BaseUrl}/pg/v4/payment/verify.json", body, cancellationToken);

            var payload = await ReadAsync(response, cancellationToken);
            var data = ReadObject<ZarinPalVerifyData>(payload?.Data);

            if (data?.Code is 100 or 101)
            {
                return new GatewayVerifyResult(true, data.RefId, data.CardPan, data.CardHash, null, null);
            }

            var error = ReadObject<ZarinPalError>(payload?.Errors);
            if (data is null && error is null)
            {
                throw new HttpRequestException(
                    "Unreadable ZarinPal verify response", null, response.StatusCode);
            }

            return new GatewayVerifyResult(false, null, null, null, error?.Code?.ToString(), error?.Message);
        }

        private static async Task<ZarinPalResponse?> ReadAsync(
            HttpResponseMessage response, CancellationToken cancellationToken)
        {
            try
            {
                return await response.Content.ReadFromJsonAsync<ZarinPalResponse>(
                    cancellationToken: cancellationToken);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static T? ReadObject<T>(JsonElement? element) where T : class =>
            element is { ValueKind: JsonValueKind.Object } value ? value.Deserialize<T>() : null;

        private sealed record ZarinPalRequestBody(
            [property: JsonPropertyName("merchant_id")] string MerchantId,
            [property: JsonPropertyName("amount")] long Amount,
            [property: JsonPropertyName("description")] string Description,
            [property: JsonPropertyName("callback_url")] string CallbackUrl);

        private sealed record ZarinPalVerifyBody(
            [property: JsonPropertyName("merchant_id")] string MerchantId,
            [property: JsonPropertyName("amount")] long Amount,
            [property: JsonPropertyName("authority")] string Authority);

        private sealed class ZarinPalResponse
        {
            [JsonPropertyName("data")] public JsonElement? Data { get; set; }
            [JsonPropertyName("errors")] public JsonElement? Errors { get; set; }
        }

        private sealed class ZarinPalRequestData
        {
            [JsonPropertyName("code")] public int Code { get; set; }
            [JsonPropertyName("authority")] public string? Authority { get; set; }
            [JsonPropertyName("fee")] public int? Fee { get; set; }
        }

        private sealed class ZarinPalVerifyData
        {
            [JsonPropertyName("code")] public int Code { get; set; }
            [JsonPropertyName("ref_id")] public long? RefId { get; set; }
            [JsonPropertyName("card_pan")] public string? CardPan { get; set; }
            [JsonPropertyName("card_hash")] public string? CardHash { get; set; }
        }

        private sealed class ZarinPalError
        {
            [JsonPropertyName("code")] public int? Code { get; set; }
            [JsonPropertyName("message")] public string? Message { get; set; }
        }
    }
}
