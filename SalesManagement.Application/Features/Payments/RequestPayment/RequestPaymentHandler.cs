using Microsoft.Extensions.Logging;
using SalesManagement.Application.Common.Interfaces;
using SalesManagement.Domain.Common.Exceptions;
using SalesManagement.Domain.Features.Payments;

namespace SalesManagement.Application.Features.Payments.RequestPayment
{
    public class RequestPaymentHandler(
        IPaymentRepository paymentRepository,
        IPaymentGateway paymentGateway,
        IPaymentCallbackUrlProvider callbackUrlProvider,
        ILogger<RequestPaymentHandler> logger)
    {
        public async Task<RequestPaymentResult> Handle(
            RequestPaymentCommand cmd, CancellationToken cancellationToken = default)
        {
            var payment = Payment.Create(
                cmd.PartyRef, cmd.TableName, cmd.TargetRef, paymentGateway.Name, cmd.Amount, cmd.Description,
                cmd.Channel, cmd.IpAddress);

            await paymentRepository.InsertAsync(payment, cancellationToken);

            var callbackUrl = callbackUrlProvider.Build(payment.Id);
            var gatewayResult = await paymentGateway.RequestAsync(
                payment.Amount, payment.Description, callbackUrl, cancellationToken);

            if (!gatewayResult.Success || gatewayResult.Authority is null)
            {
                payment = payment.MarkRequestFailed(gatewayResult.ErrorCode, gatewayResult.ErrorMessage);
                await paymentRepository.UpdateAsync(payment, cancellationToken);
                logger.LogWarning(
                    "درخواست پرداخت ناموفق بود PaymentId={PaymentId} Error={ErrorCode}",
                    payment.Id, gatewayResult.ErrorCode);
                throw new DomainException("امکان اتصال به درگاه پرداخت وجود ندارد");
            }

            payment = payment.MarkSentToGateway(gatewayResult.Authority, gatewayResult.Fee);
            await paymentRepository.UpdateAsync(payment, cancellationToken);

            logger.LogInformation("درخواست پرداخت ساخته شد PaymentId={PaymentId}", payment.Id);

            return new RequestPaymentResult(payment.Id, gatewayResult.PaymentUrl!);
        }
    }
}