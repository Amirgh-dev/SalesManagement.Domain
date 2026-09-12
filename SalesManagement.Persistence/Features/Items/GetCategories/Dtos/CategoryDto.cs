namespace SalesManagement.Persistence.Features.Items.GetCategories.Dtos
{
    public class CategoryDto
    {
        public int GroupingId { get; set; }
        public string? Code { get; set; }
        public string? Title { get; set; }
        public int? ParentGroupRef { get; set; }
        public string? EntityType { get; set; }
    }
}
