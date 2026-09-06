namespace SalesManagement.Persistence.Features.Items.GetCategories.Dtos
{
    /// <summary>ردیف خام کوئری دسته‌ها (GNR.Grouping).</summary>
    public class CategoryDto
    {
        public int GroupingId { get; set; }
        public string? Code { get; set; }
        public string? Title { get; set; }
        public int? ParentGroupRef { get; set; }
        public string? EntityType { get; set; }
    }
}
