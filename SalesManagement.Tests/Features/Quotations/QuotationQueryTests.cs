using SalesManagement.Domain.Common;
using SalesManagement.Domain.Features.Quotations;

namespace SalesManagement.Tests.Features.Quotations
{
    public class QuotationQueryTests
    {
        private static QuotationQuery Make(
            string? scope = null,
            string? fields = null,
            IEnumerable<(string?, string?, string?)>? filters = null,
            (string? Term, string? Fields) search = default,
            string? orderBy = null,
            bool descending = true,
            int? skip = null,
            int? take = null,
            int? page = null,
            int? pageSize = null)
            => QuotationQuery.Create(scope, fields, filters, search, orderBy, descending, skip, take, page, pageSize);

        [Theory]
        [InlineData(null, QuotationScope.Both)]
        [InlineData("", QuotationScope.Both)]
        [InlineData("both", QuotationScope.Both)]
        [InlineData("string", QuotationScope.Both)]
        [InlineData("garbage", QuotationScope.Both)]
        [InlineData("quotations", QuotationScope.Quotations)]
        [InlineData("items", QuotationScope.Items)]
        [InlineData("  ITEMS  ", QuotationScope.Items)]
        [InlineData("Quotations", QuotationScope.Quotations)]
        public void Scope_is_parsed_leniently(string? scope, QuotationScope expected)
        {
            Assert.Equal(expected, Make(scope: scope).Scope);
        }

        [Fact]
        public void Fields_null_gives_empty_list()
        {
            Assert.Empty(Make(fields: null).Fields);
        }

        [Fact]
        public void Fields_are_split_trimmed_and_deduplicated()
        {
            var q = Make(fields: "number, price , number ,  netPrice");
            Assert.Equal(new[] { "number", "price", "netPrice" }, q.Fields);
        }

        [Fact]
        public void Fields_placeholder_string_token_is_ignored()
        {
            Assert.Equal(new[] { "number", "price" }, Make(fields: "number,string,price").Fields);
            Assert.Empty(Make(fields: "string").Fields);
        }

        [Fact]
        public void Filters_null_gives_empty_list()
        {
            Assert.Empty(Make(filters: null).Filters);
        }

        [Theory]
        [InlineData(null, "eq", "10")]
        [InlineData("", "eq", "10")]
        [InlineData("string", "eq", "10")]
        [InlineData("number", "eq", null)]
        [InlineData("number", "eq", "string")]
        [InlineData("number", "weird", "10")]
        public void Filters_with_blank_or_unknown_parts_are_dropped(string? field, string? op, string? value)
        {
            Assert.Empty(Make(filters: new (string?, string?, string?)[] { (field, op, value) }).Filters);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Blank_op_defaults_to_eq(string? op)
        {
            var f = Assert.Single(Make(filters: new (string?, string?, string?)[] { ("number", op, "10") }).Filters);
            Assert.Equal(FilterOp.Eq, f.Op);
        }

        [Fact]
        public void Filter_is_trimmed_and_default_op_is_eq()
        {
            var f = Assert.Single(Make(filters: new (string?, string?, string?)[] { ("  number  ", "", "  10  ") }).Filters);
            Assert.Equal("number", f.Field);
            Assert.Equal(FilterOp.Eq, f.Op);
            Assert.Equal("10", f.Value);
        }

        [Theory]
        [InlineData("eq", FilterOp.Eq)]
        [InlineData("ne", FilterOp.Ne)]
        [InlineData("gt", FilterOp.Gt)]
        [InlineData("gte", FilterOp.Gte)]
        [InlineData("lt", FilterOp.Lt)]
        [InlineData("lte", FilterOp.Lte)]
        [InlineData("contains", FilterOp.Contains)]
        [InlineData("CONTAINS", FilterOp.Contains)]
        public void Filter_operators_are_parsed(string op, FilterOp expected)
        {
            var f = Assert.Single(Make(filters: new (string?, string?, string?)[] { ("price", op, "1") }).Filters);
            Assert.Equal(expected, f.Op);
        }

        [Fact]
        public void Only_valid_filters_survive_when_mixed()
        {
            var q = Make(filters: new (string?, string?, string?)[]
            {
                ("number", "gte", "10"),
                ("", "eq", "x"),
                ("price", "bogus", "5"),
                ("customerName", "contains", "ali"),
            });

            Assert.Equal(2, q.Filters.Count);
            Assert.Contains(q.Filters, x => x.Field == "number" && x.Op == FilterOp.Gte);
            Assert.Contains(q.Filters, x => x.Field == "customerName" && x.Op == FilterOp.Contains);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("string")]
        public void Search_is_null_when_term_is_blank_or_placeholder(string? term)
        {
            Assert.Null(Make(search: (term, "number")).Search);
        }

        [Fact]
        public void Search_term_is_trimmed()
        {
            Assert.Equal("lamp", Make(search: ("  lamp  ", null)).Search!.Term);
        }

        [Fact]
        public void Search_uses_its_own_fields_when_given()
        {
            var q = Make(fields: "number", search: ("x", "customerName, price"));
            Assert.Equal(new[] { "customerName", "price" }, q.Search!.Fields);
        }

        [Fact]
        public void Search_falls_back_to_top_level_fields_when_its_fields_are_empty()
        {
            var q = Make(fields: "number,price", search: ("x", null));
            Assert.Equal(new[] { "number", "price" }, q.Search!.Fields);
        }

        [Fact]
        public void Search_falls_back_to_top_level_fields_when_its_fields_are_only_placeholder()
        {
            var q = Make(fields: "number", search: ("x", "string"));
            Assert.Equal(new[] { "number" }, q.Search!.Fields);
        }

        [Fact]
        public void Search_fields_empty_and_no_top_level_fields_stays_empty()
        {
            var q = Make(fields: null, search: ("x", null));
            Assert.Empty(q.Search!.Fields);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("string")]
        public void OrderBy_blank_or_placeholder_becomes_null(string? orderBy)
        {
            Assert.Null(Make(orderBy: orderBy).OrderBy);
        }

        [Fact]
        public void OrderBy_is_trimmed()
        {
            Assert.Equal("number", Make(orderBy: "  number  ").OrderBy);
        }

        [Fact]
        public void Descending_flag_is_passed_through()
        {
            Assert.True(Make(descending: true).Descending);
            Assert.False(Make(descending: false).Descending);
        }

        [Fact]
        public void Default_paging_is_skip_0_take_100()
        {
            var q = Make();
            Assert.Equal(0, q.Skip);
            Assert.Equal(100, q.Take);
        }

        [Theory]
        [InlineData(0, 20, 0, 20)]
        [InlineData(5, 20, 5, 20)]
        [InlineData(-3, 20, 0, 20)]
        [InlineData(10, null, 10, 100)]
        [InlineData(0, 0, 0, 100)]
        [InlineData(0, 9999, 0, 500)]
        public void Skip_take_mode(int? skip, int? take, int expectedSkip, int expectedTake)
        {
            var q = Make(skip: skip, take: take);
            Assert.Equal(expectedSkip, q.Skip);
            Assert.Equal(expectedTake, q.Take);
        }

        [Theory]
        [InlineData(1, 10, 0, 10)]
        [InlineData(2, 10, 10, 10)]
        [InlineData(3, 25, 50, 25)]
        [InlineData(1, null, 0, 100)]
        [InlineData(2, 0, 100, 100)]
        [InlineData(2, 99999, 500, 500)]
        public void Page_mode(int? page, int? pageSize, int expectedSkip, int expectedTake)
        {
            var q = Make(page: page, pageSize: pageSize);
            Assert.Equal(expectedSkip, q.Skip);
            Assert.Equal(expectedTake, q.Take);
        }

        [Fact]
        public void PageSize_without_page_is_ignored_on_quotation_query()
        {
            var q = Make(pageSize: 15);
            Assert.Equal(100, q.Take);
            Assert.Equal(0, q.Skip);
        }

        [Fact]
        public void The_full_swagger_placeholder_body_produces_a_safe_default_query()
        {
            var q = Make(
                scope: "string",
                fields: "string",
                filters: new[] { ((string?)"string", (string?)"string", (string?)"string") },
                search: ("string", "string"),
                orderBy: "string",
                descending: true,
                skip: 0, take: 0, page: 0, pageSize: 0);

            Assert.Equal(QuotationScope.Both, q.Scope);
            Assert.Empty(q.Fields);
            Assert.Empty(q.Filters);
            Assert.Null(q.Search);
            Assert.Null(q.OrderBy);
            Assert.Equal(0, q.Skip);
            Assert.Equal(100, q.Take);
        }
    }
}
