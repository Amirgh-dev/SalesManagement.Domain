using System.ComponentModel;

namespace SalesManagement.Domain.Enums
{
    public enum BouncedCheckRegisteredStep
    {
        [Description("ارجاع به دفتر وکالت")] ReferralLawFirm = 1,
        [Description("ارسال دادخواست")] SubmitPetition = 2,
        [Description("صدرو اجرای حکم")] IssuanceExecutionOrder = 3,
    }
}