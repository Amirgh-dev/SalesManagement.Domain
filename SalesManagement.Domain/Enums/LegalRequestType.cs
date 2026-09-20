using System.ComponentModel;

namespace SalesManagement.Domain.Enums
{
    public enum LegalRequestType
    {
        [Description("چک برگشتی ثبت شده")] BouncedCheckRegistered = 1,
        [Description("چک ثبت نشده")] UnregisteredCheck = 2,
        [Description("اقدام روی رسید دریافت کالا و فاکتور فروش با امضا خریدار")] SignedInvoiceAndGoodsReceipt = 3,
        [Description("سفته")] PromissoryNote = 4
    }
}