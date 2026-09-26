namespace SalesManagement.Domain.Features.SaleTypeDiscounts
{
    public class SaleTypeDiscount
    {
        public int Id { get; }
        public int SaleTypeRef { get; }
        public string? Title { get; }
        public decimal? Discount { get; }
        public int? IsCash { get; }
        public bool? BlackList { get; }

        private SaleTypeDiscount(
            int id, int saleTypeRef, string? title, decimal? discount, int? isCash, bool? blackList)
        {
            Id = id;
            SaleTypeRef = saleTypeRef;
            Title = title;
            Discount = discount;
            IsCash = isCash;
            BlackList = blackList;
        }

        public static SaleTypeDiscount Reconstitute(
            int id, int saleTypeRef, string? title, decimal? discount, int? isCash, bool? blackList) =>
            new(id, saleTypeRef, title, discount, isCash, blackList);
    }
}
