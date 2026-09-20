using System.ComponentModel;

namespace SalesManagement.Domain.Enums
{
    public enum UnregisteredCheckOrSignedDocumentsStep
    {
        [Description("اخذ وکالت")] ObtainPowerOfAttorney = 1,
        [Description("ارسال دادخواست")] SubmitPetition = 2,
        [Description("ارسال لایحه دفاعیه")] SubmitDefenseBrief = 3,
        [Description("تشکیل جلسه دادگاه")] CourtSessionHeld = 4,
        [Description("تهیه و ارسال سیستمی لایحه دفاعیه توسط وکیل")] SubmitDefenseBriefByAttorney = 5,
        [Description("ارسال درخواست حضور وکیل در دادگاه")] RequestAttorneyPresence = 6
    }
}