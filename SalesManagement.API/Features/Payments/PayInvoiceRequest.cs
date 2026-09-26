using System.ComponentModel.DataAnnotations;

namespace SalesManagement.API.Features.Payments
{
    public class PayInvoiceRequest
    {
        [Required][Range(1, int.MaxValue)] public int? InvoiceId { get; init; }
        [Required][MaxLength(30)] public string Channel { get; init; } = "";
    }
}
