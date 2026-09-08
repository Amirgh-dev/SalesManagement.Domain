namespace SalesManagement.Persistence.Features.Invoices.GetVendorInvoices.Dtos
{
    public class InvoiceItemDto
    {
        public int InvoiceItemId { get; set; }
        public int InvoiceRef { get; set; }
        public int RowId { get; set; }
        public int ItemRef { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemTitle { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal Fee { get; set; }
        public decimal Price { get; set; }
        public decimal Discount { get; set; }
        public decimal NetPrice { get; set; }
        public string? Description { get; set; }
    }
}
