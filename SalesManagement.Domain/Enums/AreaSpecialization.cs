using System.ComponentModel;

namespace SalesManagement.Domain.Enums
{
    public enum AreaSpecialization
    {
        [Description("عمومی")] General = 1,
        [Description("صنعتی")] Industrial = 2,
        [Description("روشنایی")] Lighting = 3,
        [Description("لوستر")] Chandelier = 4,
        [Description("ابزار")] Tools = 5,
        [Description("سایر")] Other = 6
    }
}