

namespace SalesManagement.Domain.Entities
{
    public class InvoiceItem
    {
        public int Id { get; private set; }
        public int RowId { get; private set; }
        public int ItemRef { get; private set; }
        public string ItemCode { get; private set; } = string.Empty;
        public string ItemTitle { get; private set; } = string.Empty;
        public decimal Quantity { get; private set; }
        public decimal Fee { get; private set; }
        public decimal Price { get; private set; }
        public decimal Discount { get; private set; }
        public decimal NetPrice { get; private set; }
        public string? Description { get; private set; }

        private InvoiceItem() {}

        public static InvoiceItem Reconstitute(
            int id, int rowId, int itemRef, string itemCode, string itemTitle,
            decimal quantity, decimal fee, decimal price, decimal discount,
            decimal netPrice, string? description)
        {
            return new InvoiceItem
            {
                Id = id,
                RowId = rowId,
                ItemRef = itemRef,
                ItemCode = itemCode,
                ItemTitle = itemTitle,
                Quantity = quantity,
                Fee = fee,
                Price = price,
                Discount = discount,
                NetPrice = netPrice,
                Description = description
            };
        }
    }
}
