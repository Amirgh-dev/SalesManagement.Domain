using Microsoft.Extensions.Logging;
using SalesManagement.Application.Common.Interfaces;
using SalesManagement.Domain.Common.Exceptions;
using SalesManagement.Domain.Enums;
using SalesManagement.Domain.Features.Payments;

namespace SalesManagement.Application.Features.Payments.VerifyPayment
{
    public class VerifyPaymentHandler(
        IPaymentRepository paymentRepository,
        IPaymentGateway paymentGateway,
        ILogger<VerifyPaymentHandler> logger)
    {
        public async Task<VerifyPaymentResult> Handle(
            VerifyPaymentCommand cmd, CancellationToken cancellationToken = default)
        {
            var payment = await paymentRepository.GetByIdAsync(cmd.PaymentId, cancellationToken)
                          ?? throw new DomainException("تراکنش پیدا نشد");

            if (payment.State is PaymentState.Verified or PaymentState.VerifyFailed
                or PaymentState.CanceledByUser or PaymentState.RequestFailed)
            {
                return new VerifyPaymentResult(payment.State == PaymentState.Verified, payment.TrackingCode!);
            }

            if (payment.State != PaymentState.SentToGateway || payment.Authority is null)
                throw new DomainException("این تراکنش هنوز به درگاه پرداخت ارسال نشده است");

            if (!string.Equals(payment.Authority, cmd.Authority, StringComparison.Ordinal))
                throw new DomainException("اطلاعات بازگشتی از درگاه پرداخت نامعتبر است");

            if (!string.Equals(cmd.Status, "OK", StringComparison.OrdinalIgnoreCase))
            {
                payment = payment.MarkCanceledByUser();
                await paymentRepository.UpdateAsync(payment, cancellationToken);
                logger.LogInformation("پرداخت توسط کاربر لغو شد PaymentId={PaymentId}", payment.Id);
                return new VerifyPaymentResult(false, payment.TrackingCode!);
            }

            var verifyResult = await paymentGateway.VerifyAsync(
                payment.Amount, payment.Authority, cancellationToken);

            if (!verifyResult.Success || verifyResult.RefId is null)
            {
                payment = payment.MarkVerifyFailed(verifyResult.ErrorCode, verifyResult.ErrorMessage);
                await paymentRepository.UpdateAsync(payment, cancellationToken);
                logger.LogWarning(
                    "وریفای پرداخت ناموفق بود PaymentId={PaymentId} Error={ErrorCode}",
                    payment.Id, verifyResult.ErrorCode);
                return new VerifyPaymentResult(false, payment.TrackingCode!);
            }

            payment = payment.MarkVerified(verifyResult.RefId.Value, verifyResult.CardPan, verifyResult.CardHash);
            await paymentRepository.UpdateAsync(payment, cancellationToken);

            logger.LogInformation("پرداخت تایید شد PaymentId={PaymentId} RefId={RefId}", payment.Id, payment.RefId);

            return new VerifyPaymentResult(true, payment.TrackingCode!);
        }
    }
}
