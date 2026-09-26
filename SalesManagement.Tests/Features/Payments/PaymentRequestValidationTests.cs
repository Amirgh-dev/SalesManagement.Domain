using System.ComponentModel.DataAnnotations;
using SalesManagement.API.Features.Payments;

namespace SalesManagement.Tests.Features.Payments
{
    public class PaymentRequestValidationTests
    {
        private static List<ValidationResult> Validate(object model)
        {
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
            return results;
        }

        private static bool Fails(object model, string member) =>
            Validate(model).Any(r => r.MemberNames.Contains(member));

        [Fact]
        public void Valid_payment_request_passes()
        {
            var model = new RequestPaymentRequest { Amount = 1000, Description = "d", Channel = "web" };

            Assert.Empty(Validate(model));
        }

        [Fact]
        public void Payment_request_at_max_lengths_passes()
        {
            var model = new RequestPaymentRequest
            {
                Amount = long.MaxValue,
                Description = new string('d', 500),
                Channel = new string('c', 30),
            };

            Assert.Empty(Validate(model));
        }

        [Fact]
        public void Payment_request_amount_is_required()
        {
            Assert.True(Fails(new RequestPaymentRequest { Amount = null, Description = "d", Channel = "web" }, "Amount"));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(long.MinValue)]
        public void Payment_request_amount_must_be_positive(long amount)
        {
            Assert.True(Fails(new RequestPaymentRequest { Amount = amount, Description = "d", Channel = "web" }, "Amount"));
        }

        [Fact]
        public void Payment_request_amount_of_one_is_allowed()
        {
            Assert.False(Fails(new RequestPaymentRequest { Amount = 1, Description = "d", Channel = "web" }, "Amount"));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Payment_request_description_is_required(string? description)
        {
            var model = new RequestPaymentRequest { Amount = 1, Description = description!, Channel = "web" };

            Assert.True(Fails(model, "Description"));
        }

        [Fact]
        public void Payment_request_description_over_500_chars_fails()
        {
            var model = new RequestPaymentRequest { Amount = 1, Description = new string('d', 501), Channel = "web" };

            Assert.True(Fails(model, "Description"));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Payment_request_channel_is_required(string? channel)
        {
            var model = new RequestPaymentRequest { Amount = 1, Description = "d", Channel = channel! };

            Assert.True(Fails(model, "Channel"));
        }

        [Fact]
        public void Payment_request_channel_over_30_chars_fails()
        {
            var model = new RequestPaymentRequest { Amount = 1, Description = "d", Channel = new string('c', 31) };

            Assert.True(Fails(model, "Channel"));
        }

        [Fact]
        public void Payment_request_does_not_expose_table_or_target_or_party()
        {
            var names = typeof(RequestPaymentRequest).GetProperties().Select(p => p.Name).ToHashSet();

            Assert.Equal(new HashSet<string> { "Amount", "Description", "Channel" }, names);
        }

        [Fact]
        public void Valid_invoice_request_passes()
        {
            Assert.Empty(Validate(new PayInvoiceRequest { InvoiceId = 5, Channel = "web" }));
        }

        [Fact]
        public void Invoice_request_invoice_id_is_required()
        {
            Assert.True(Fails(new PayInvoiceRequest { InvoiceId = null, Channel = "web" }, "InvoiceId"));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(int.MinValue)]
        public void Invoice_request_invoice_id_must_be_positive(int invoiceId)
        {
            Assert.True(Fails(new PayInvoiceRequest { InvoiceId = invoiceId, Channel = "web" }, "InvoiceId"));
        }

        [Fact]
        public void Invoice_request_accepts_int_max()
        {
            Assert.False(Fails(new PayInvoiceRequest { InvoiceId = int.MaxValue, Channel = "web" }, "InvoiceId"));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Invoice_request_channel_is_required(string? channel)
        {
            Assert.True(Fails(new PayInvoiceRequest { InvoiceId = 1, Channel = channel! }, "Channel"));
        }

        [Fact]
        public void Invoice_request_channel_over_30_chars_fails()
        {
            Assert.True(Fails(new PayInvoiceRequest { InvoiceId = 1, Channel = new string('c', 31) }, "Channel"));
        }

        [Fact]
        public void Invoice_request_does_not_expose_amount_or_party()
        {
            var names = typeof(PayInvoiceRequest).GetProperties().Select(p => p.Name).ToHashSet();

            Assert.Equal(new HashSet<string> { "InvoiceId", "Channel" }, names);
        }
    }
}
