using System.Globalization;
using SalesManagement.Domain.Common.Exceptions;
using SalesManagement.Domain.Enums;

namespace SalesManagement.Domain.Features.Payments
{
    public sealed record Payment
    {
        public Guid Id { get; }
        public int PartyRef { get; }
        public string? TableName { get; }
        public int? TargetRef { get; }
        public string Gateway { get; }
        public long Amount { get; }
        public string Description { get; }
        public string? Authority { get; private init; }
        public PaymentState State { get; private init; }
        public bool IsPay { get; private init; }
        public bool IsOk { get; private init; }
        public int Modifier { get; private init; }
        public DateTime LastModificationDate { get; private init; }
        public long? RefId { get; private init; }
        public string? TrackingCode { get; }
        public string? CardPan { get; private init; }
        public string? CardHash { get; private init; }
        public int? Fee { get; private init; }
        public string? ErrorCode { get; private init; }
        public string? ErrorMessage { get; private init; }
        public string Channel { get; }
        public string IpAddress { get; }
        public DateTime CreationDate { get; }
        public DateTime? VerifiedDate { get; private init; }
        public DateTime? LastModifiedDate { get; private init; }

        private Payment(
            Guid id, int partyRef, string? tableName, int? targetRef, string gateway, long amount,
            string description, string? authority, PaymentState state, bool isPay, bool isOk,
            int modifier, DateTime lastModificationDate, long? refId, string? trackingCode,
            string? cardPan, string? cardHash, int? fee, string? errorCode, string? errorMessage,
            string channel, string ipAddress, DateTime creationDate, DateTime? verifiedDate,
            DateTime? lastModifiedDate)
        {
            Id = id;
            PartyRef = partyRef;
            TableName = tableName;
            TargetRef = targetRef;
            Gateway = gateway;
            Amount = amount;
            Description = description;
            Authority = authority;
            State = state;
            IsPay = isPay;
            IsOk = isOk;
            Modifier = modifier;
            LastModificationDate = lastModificationDate;
            RefId = refId;
            TrackingCode = trackingCode;
            CardPan = cardPan;
            CardHash = cardHash;
            Fee = fee;
            ErrorCode = errorCode;
            ErrorMessage = errorMessage;
            Channel = channel;
            IpAddress = ipAddress;
            CreationDate = creationDate;
            VerifiedDate = verifiedDate;
            LastModifiedDate = lastModifiedDate;
        }

        public static Payment Create(
            int partyRef, string? tableName, int? targetRef, string gateway, long amount,
            string description, string channel, string ipAddress)
        {
            if (partyRef <= 0) throw new DomainException("شناسه کاربر نامعتبر است");
            if (string.IsNullOrWhiteSpace(gateway)) throw new DomainException("درگاه پرداخت نامشخص است");
            if (amount <= 0) throw new DomainException("مبلغ پرداخت نامعتبر است");
            if (string.IsNullOrWhiteSpace(description)) throw new DomainException("توضیح پرداخت نمی‌تواند خالی باشد");
            if (string.IsNullOrWhiteSpace(channel)) throw new DomainException("مبدا پرداخت نامشخص است");
            if (string.IsNullOrWhiteSpace(ipAddress)) throw new DomainException("آی‌پی درخواست نامشخص است");
            if (gateway.Length > 50) throw new DomainException("نام درگاه پرداخت بیش از حد مجاز است");
            if (description.Length > 500) throw new DomainException("توضیح پرداخت نباید بیش از ۵۰۰ کاراکتر باشد");
            if (tableName is { Length: > 200 }) throw new DomainException("نام جدول مقصد بیش از حد مجاز است");
            if (channel.Length > 30) throw new DomainException("مبدا پرداخت بیش از حد مجاز است");
            if (ipAddress.Length > 45) throw new DomainException("آی‌پی درخواست نامعتبر است");

            var now = DateTime.Now;
            var trackingCode = now.ToString("yyMMddHHmmss", CultureInfo.InvariantCulture)
                               + Random.Shared.Next(100, 1000).ToString(CultureInfo.InvariantCulture);

            return new Payment(
                Guid.NewGuid(), partyRef, tableName, targetRef, gateway, amount, description,
                authority: null, state: PaymentState.Created, isPay: false, isOk: false,
                modifier: 0, lastModificationDate: now, refId: null, trackingCode: trackingCode,
                cardPan: null, cardHash: null, fee: null, errorCode: null, errorMessage: null,
                channel: channel, ipAddress: ipAddress, creationDate: now, verifiedDate: null,
                lastModifiedDate: now);
        }

        public static Payment Reconstitute(
            Guid id, int partyRef, string? tableName, int? targetRef, string gateway, long amount,
            string description, string? authority, PaymentState state, bool isPay, bool isOk,
            int modifier, DateTime lastModificationDate, long? refId, string? trackingCode,
            string? cardPan, string? cardHash, int? fee, string? errorCode, string? errorMessage,
            string channel, string ipAddress, DateTime creationDate, DateTime? verifiedDate,
            DateTime? lastModifiedDate) =>
            new(id, partyRef, tableName, targetRef, gateway, amount, description, authority, state,
                isPay, isOk, modifier, lastModificationDate, refId, trackingCode, cardPan, cardHash,
                fee, errorCode, errorMessage, channel, ipAddress, creationDate, verifiedDate,
                lastModifiedDate);

        public Payment MarkSentToGateway(string authority, int? fee) => this with
        {
            Authority = authority,
            State = PaymentState.SentToGateway,
            Fee = fee,
            LastModifiedDate = DateTime.Now,
        };

        public Payment MarkRequestFailed(string? errorCode, string? errorMessage) => this with
        {
            State = PaymentState.RequestFailed,
            ErrorCode = Truncate(errorCode, 20),
            ErrorMessage = Truncate(errorMessage, 500),
            LastModifiedDate = DateTime.Now,
        };

        public Payment MarkCanceledByUser() => this with
        {
            State = PaymentState.CanceledByUser,
            LastModifiedDate = DateTime.Now,
        };

        public Payment MarkVerified(long refId, string? cardPan, string? cardHash) => this with
        {
            State = PaymentState.Verified,
            IsPay = true,
            RefId = refId,
            CardPan = cardPan,
            CardHash = cardHash,
            VerifiedDate = DateTime.Now,
            LastModifiedDate = DateTime.Now,
        };

        public Payment MarkVerifyFailed(string? errorCode, string? errorMessage) => this with
        {
            State = PaymentState.VerifyFailed,
            ErrorCode = Truncate(errorCode, 20),
            ErrorMessage = Truncate(errorMessage, 500),
            LastModifiedDate = DateTime.Now,
        };

        public Payment MarkApproved(int adminUserId) => this with
        {
            IsOk = true,
            Modifier = adminUserId,
            LastModificationDate = DateTime.Now,
        };

        private static string? Truncate(string? value, int maxLength) =>
            value is null || value.Length <= maxLength ? value : value[..maxLength];
    }
}
