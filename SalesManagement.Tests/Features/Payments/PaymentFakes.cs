using SalesManagement.Application.Common.Interfaces;
using SalesManagement.Domain.Enums;
using SalesManagement.Domain.Features.Invoices;
using SalesManagement.Domain.Features.Payments;

namespace SalesManagement.Tests.Features.Payments
{
    internal sealed class FakePaymentRepository : IPaymentRepository
    {
        private readonly Dictionary<Guid, Payment> _store = [];

        public int InsertCalls { get; private set; }
        public int UpdateCalls { get; private set; }
        public IReadOnlyCollection<Payment> All => _store.Values;

        public Task InsertAsync(Payment payment, CancellationToken cancellationToken = default)
        {
            InsertCalls++;
            _store[payment.Id] = payment;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Payment payment, CancellationToken cancellationToken = default)
        {
            UpdateCalls++;
            _store[payment.Id] = payment;
            return Task.CompletedTask;
        }

        public Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_store.GetValueOrDefault(id));

        public Task<Payment?> GetByAuthorityAsync(string authority, CancellationToken cancellationToken = default) =>
            Task.FromResult(_store.Values.FirstOrDefault(p => p.Authority == authority));

        public Task<bool> ExistsVerifiedAsync(
            int partyRef, string tableName, int targetRef, CancellationToken cancellationToken = default) =>
            Task.FromResult(_store.Values.Any(p =>
                p.PartyRef == partyRef && p.TableName == tableName && p.TargetRef == targetRef
                && p.State == PaymentState.Verified));

        public Payment Seed(Payment payment)
        {
            _store[payment.Id] = payment;
            return payment;
        }

        public void Clear()
        {
            _store.Clear();
            InsertCalls = 0;
            UpdateCalls = 0;
        }
    }

    internal sealed class FakePaymentGateway : IPaymentGateway
    {
        public string Name => "FakeGateway";

        public GatewayRequestResult RequestResult { get; set; } =
            new(true, "AUTH-1", "https://pay.example/AUTH-1", 1000, null, null);

        public GatewayVerifyResult VerifyResult { get; set; } =
            new(true, 123456, "603799******1234", "card-hash", null, null);

        public int RequestCalls { get; private set; }
        public int VerifyCalls { get; private set; }
        public long? LastRequestAmount { get; private set; }
        public string? LastRequestDescription { get; private set; }
        public string? LastCallbackUrl { get; private set; }
        public long? LastVerifyAmount { get; private set; }
        public string? LastVerifyAuthority { get; private set; }
        public CancellationToken LastRequestToken { get; private set; }
        public CancellationToken LastVerifyToken { get; private set; }
        public Exception? RequestException { get; set; }
        public Exception? VerifyException { get; set; }

        public Task<GatewayRequestResult> RequestAsync(
            long amount, string description, string callbackUrl, CancellationToken cancellationToken = default)
        {
            RequestCalls++;
            LastRequestToken = cancellationToken;
            if (RequestException is not null) throw RequestException;
            LastRequestAmount = amount;
            LastRequestDescription = description;
            LastCallbackUrl = callbackUrl;
            return Task.FromResult(RequestResult);
        }

        public Task<GatewayVerifyResult> VerifyAsync(
            long amount, string authority, CancellationToken cancellationToken = default)
        {
            VerifyCalls++;
            LastVerifyToken = cancellationToken;
            if (VerifyException is not null) throw VerifyException;
            LastVerifyAmount = amount;
            LastVerifyAuthority = authority;
            return Task.FromResult(VerifyResult);
        }
    }

    internal sealed class FakeCallbackUrlProvider : IPaymentCallbackUrlProvider
    {
        public string Build(Guid paymentId) => $"https://api.example/api/payments/callback?paymentId={paymentId}";
    }

    internal sealed class FakeInvoiceRepository : IInvoicePayableRepository
    {
        private readonly List<(PayableInvoice Invoice, int OwnerPartyRef)> _invoices = [];

        public void Add(PayableInvoice invoice, int ownerPartyRef) => _invoices.Add((invoice, ownerPartyRef));

        public void Clear() => _invoices.Clear();

        public Task<PayableInvoice?> GetAsync(
            int invoiceId, int partyRef, CancellationToken cancellationToken = default) =>
            Task.FromResult(
                _invoices.Where(x => x.Invoice.InvoiceId == invoiceId && x.OwnerPartyRef == partyRef)
                    .Select(x => (PayableInvoice?)x.Invoice)
                    .FirstOrDefault());
    }
}
