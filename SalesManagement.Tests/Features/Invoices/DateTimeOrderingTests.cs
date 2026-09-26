using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;
using SalesManagement.Domain.Features.Invoices;
using SalesManagement.Domain.Features.Quotations;
using SalesManagement.Persistence.Features.Invoices.QueryInvoices;
using SalesManagement.Persistence.Features.Quotations.QueryQuotations;

namespace SalesManagement.Tests.Features.Invoices
{
    public class DateTimeOrderingTests
    {
        private static InvoiceQuery Invoice(string? orderBy, bool descending) =>
            InvoiceQuery.Create(null, null, null, (null, null), orderBy, descending, null, null, null, null);

        private static QuotationQuery Quotation(string? orderBy, bool descending) =>
            QuotationQuery.Create(null, null, null, (null, null), orderBy, descending, null, null, null, null);

        private static string InvoiceOrder(string? orderBy, bool descending = true) =>
            (string)typeof(InvoiceQueryRepository)
                .GetMethod("ResolveOrderBy", BindingFlags.NonPublic | BindingFlags.Static)!
                .Invoke(null, [Invoice(orderBy, descending), true])!;

        private static string QuotationOrder(string? orderBy, bool descending = true) =>
            (string)typeof(QuotationQueryRepository)
                .GetMethod("ResolveOrderBy", BindingFlags.NonPublic | BindingFlags.Static)!
                .Invoke(null, [Quotation(orderBy, descending), true])!;

        private static IDictionary Fields(Type repository, string name) =>
            (IDictionary)repository.GetField(name, BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;

        private static string ExprOf(IDictionary fields, string key)
        {
            var tuple = fields[key]!;
            return (string)tuple.GetType().GetField("Item1")!.GetValue(tuple)!;
        }

        private static string[] Columns(string orderBy) =>
            orderBy.Split(',').Select(p => Regex.Replace(p.Trim(), @"\s+(ASC|DESC)$", "")).ToArray();

        [Fact]
        public void Invoices_expose_the_real_creation_timestamp_next_to_the_business_date()
        {
            var fields = Fields(typeof(InvoiceQueryRepository), "InvoiceFields");

            Assert.Equal("i.Date", ExprOf(fields, "date"));
            Assert.Equal("i.CreationDate", ExprOf(fields, "creationDate"));
        }

        [Fact]
        public void Quotations_expose_the_real_creation_timestamp_next_to_the_business_date()
        {
            var fields = Fields(typeof(QuotationQueryRepository), "QuotationFields");

            Assert.Equal("q.Date", ExprOf(fields, "date"));
            Assert.Equal("q.CreationDate", ExprOf(fields, "creationDate"));
        }

        [Theory]
        [InlineData(true, "DESC")]
        [InlineData(false, "ASC")]
        public void Ordering_invoices_by_date_breaks_ties_by_creation_time_then_id(bool descending, string dir)
        {
            var order = InvoiceOrder("date", descending);

            Assert.Equal($"i.Date {dir}, i.CreationDate {dir}, i.InvoiceId {dir}", order);
        }

        [Theory]
        [InlineData(true, "DESC")]
        [InlineData(false, "ASC")]
        public void Ordering_quotations_by_date_breaks_ties_by_creation_time_then_id(bool descending, string dir)
        {
            var order = QuotationOrder("date", descending);

            Assert.Equal($"q.Date {dir}, q.CreationDate {dir}, q.QuotationId {dir}", order);
        }

        [Fact]
        public void Ordering_by_creation_date_does_not_repeat_the_column()
        {
            Assert.Equal("i.CreationDate DESC, i.InvoiceId DESC", InvoiceOrder("creationDate"));
            Assert.Equal("q.CreationDate DESC, q.QuotationId DESC", QuotationOrder("creationDate"));
        }

        [Fact]
        public void Ordering_by_the_id_does_not_repeat_the_column()
        {
            Assert.Equal("i.InvoiceId DESC, i.CreationDate DESC", InvoiceOrder("invoiceId"));
            Assert.Equal("q.QuotationId DESC, q.CreationDate DESC", QuotationOrder("quotationId"));
        }

        [Theory]
        [InlineData("date")]
        [InlineData("number")]
        [InlineData("netPrice")]
        [InlineData("customerName")]
        [InlineData("saleTypeTitle")]
        [InlineData("creationDate")]
        [InlineData("invoiceId")]
        [InlineData("closed")]
        public void No_invoice_ordering_lists_the_same_column_twice(string field)
        {
            var columns = Columns(InvoiceOrder(field));

            Assert.Equal(columns.Length, columns.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        }

        [Theory]
        [InlineData("date")]
        [InlineData("number")]
        [InlineData("netPrice")]
        [InlineData("expirationDate")]
        [InlineData("creationDate")]
        [InlineData("quotationId")]
        [InlineData("closed")]
        public void No_quotation_ordering_lists_the_same_column_twice(string field)
        {
            var columns = Columns(QuotationOrder(field));

            Assert.Equal(columns.Length, columns.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        }

        [Theory]
        [InlineData("DATE")]
        [InlineData("Date")]
        [InlineData("CREATIONDATE")]
        public void Field_names_are_matched_case_insensitively_when_avoiding_duplicates(string field)
        {
            var columns = Columns(InvoiceOrder(field));

            Assert.Equal(columns.Length, columns.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        }

        [Fact]
        public void Every_explicit_ordering_ends_with_a_unique_column_so_paging_is_stable()
        {
            foreach (var field in new[] { "date", "number", "netPrice", "price", "tax", "customerName", "closed" })
            {
                Assert.EndsWith("i.InvoiceId DESC", InvoiceOrder(field));
                Assert.EndsWith("q.QuotationId DESC", QuotationOrder(field));
            }
        }

        [Fact]
        public void Default_and_unknown_ordering_keep_the_number_order()
        {
            Assert.Equal("i.Number DESC", InvoiceOrder(null));
            Assert.Equal("i.Number DESC", InvoiceOrder("doesNotExist"));
            Assert.Equal("q.Number DESC", QuotationOrder(null));
            Assert.Equal("q.Number DESC", QuotationOrder("doesNotExist"));
        }

        [Fact]
        public void Ordering_never_injects_user_input_into_the_sql()
        {
            Assert.Equal("i.Number DESC", InvoiceOrder("date; DROP TABLE x"));
            Assert.Equal("q.Number DESC", QuotationOrder("1); DELETE FROM x --"));
        }
    }
}
