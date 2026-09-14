using System.ComponentModel;

namespace SalesManagement.Domain.Enums
{
    public enum TypePost
    {
        [Description("فوری کی داره")] Urgent = 1,
        [Description("می خوام بفروشم")] Sale = 2,
        [Description("می خوام بخرم")] Buy = 3,
        [Description("پست")] Post = 4
    }
}
