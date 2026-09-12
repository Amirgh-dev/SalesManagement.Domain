namespace SalesManagement.Application.Common
{
    public record QueryFilterInput(string? Field, string? Op, string? Value);

    public record QuerySearchInput(string? Term, string? Fields);
}
