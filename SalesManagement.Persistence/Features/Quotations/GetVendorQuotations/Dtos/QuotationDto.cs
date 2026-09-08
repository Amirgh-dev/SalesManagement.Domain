namespace SalesManagement.Persistence.Features.Quotations.GetVendorQuotations.Dtos
{
    public class QuotationDto
    {
        public int QuotationId { get; set; }
        public int Number { get; set; }
        public DateTime Date { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public int CustomerPartyRef { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal Discount { get; set; }
        public decimal Tax { get; set; }
        public decimal NetPrice { get; set; }
        public bool Closed { get; set; }
    }
}