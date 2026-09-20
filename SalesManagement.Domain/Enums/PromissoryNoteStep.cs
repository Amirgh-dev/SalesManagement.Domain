using System.ComponentModel;

namespace SalesManagement.Domain.Enums
{
    public enum PromissoryNoteStep
    {
        [Description("اخذ وکالت")] ObtainPowerOfAttorney = 1,
        [Description("ارسال واخواست")] SubmitProtest = 2,
        [Description("ارسال دادخواست")] SubmitPetition = 3,
        [Description("ارسال لایحه دفاعیه")] SubmitDefenseBrief = 4
    }
}