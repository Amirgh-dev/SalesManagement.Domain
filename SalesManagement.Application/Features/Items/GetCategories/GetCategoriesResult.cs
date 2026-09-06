namespace SalesManagement.Application.Features.Items.GetCategories
{
    public record GetCategoriesResult(IReadOnlyList<CategoryListItem> Categories);

    public record CategoryListItem(
        int GroupingId,
        string? Code,
        string? Title,
        int? ParentGroupRef,
        bool IsTopLevel);
}
