using System.ComponentModel.DataAnnotations;

namespace SalesManagement.API.Features.Payments
{
    public class RequestPaymentRequest
    {
        [Required][Range(1, long.MaxValue)] public long? Amount { get; init; }
        [Required][MaxLength(500)] public string Description { get; init; } = "";
        [Required][MaxLength(30)] public string Channel { get; init; } = "";
    }
}
