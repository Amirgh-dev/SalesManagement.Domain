namespace SalesManagement.Domain.ValueObjects
{
    /// <summary>گروه فروش کالا (رابطه‌ی چندبه‌یک، امن برای نگه‌داری مستقیم روی Item).</summary>
    public sealed record ItemGrouping(int GroupingId, string? Code, string? Title, int? ParentGroupRef);
}
