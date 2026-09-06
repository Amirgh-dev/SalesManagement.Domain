namespace SalesManagement.Domain.ValueObjects
{
    /// <summary>
    /// ویژگی‌های فنی کالا از جدول INV.ItemPropertyAmount (هر کالا حداکثر یک ردیف).
    /// </summary>
    public sealed record ItemProperties(
        int ItemPropertyAmountId,
        string? BodyMaterial,     // جنس بدنه
        string? BodyColor,        // رنگ بدنه
        string? OpticalSpectrum,  // طیف نوری
        string? ProductModel,     // مدل محصول
        string? Power,            // توان
        string? Coverage,         // پوشش
        string? QuantityCarton);  // تعداد در کارتن
}
