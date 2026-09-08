namespace SalesManagement.Domain.Entities
{
    public class Quotation
    {
        private readonly List<QuotationItem> _items = new();

        public int Id { get; private set; }        
        public int Number { get; private set; }             
        public DateTime Date { get; private set; }   
        public DateTime? ExpirationDate { get; private set; }  
        public int CustomerPartyRef { get; private set; }      
        public string CustomerName { get; private set; } = string.Empty; 
        public decimal Price { get; private set; }
        public decimal Discount { get; private set; }
        public decimal Tax { get; private set; }
        public decimal NetPrice { get; private set; }
        public bool Closed { get; private set; }              

        public IReadOnlyList<QuotationItem> Items => _items;

        private Quotation() { }

        public static Quotation Reconstitute(
            int id, int number, DateTime date, DateTime? expirationDate,
            int customerPartyRef, string customerName,
            decimal price, decimal discount, decimal tax, decimal netPrice, bool closed,
            IEnumerable<QuotationItem> items)
        {
            var quotation = new Quotation
            {
                Id = id,
                Number = number,
                Date = date,
                ExpirationDate = expirationDate,
                CustomerPartyRef = customerPartyRef,
                CustomerName = customerName,
                Price = price,
                Discount = discount,
                Tax = tax,
                NetPrice = netPrice,
                Closed = closed
            };
            quotation._items.AddRange(items);
            return quotation;
        }
    }
}