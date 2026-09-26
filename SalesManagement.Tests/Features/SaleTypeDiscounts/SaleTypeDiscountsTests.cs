using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using SalesManagement.API.Controllers;
using SalesManagement.Application.Features.SaleTypeDiscounts.GetSaleTypeDiscounts;
using SalesManagement.Domain.Features.SaleTypeDiscounts;
using SalesManagement.Persistence.Features.SaleTypeDiscounts.GetSaleTypeDiscounts;

namespace SalesManagement.Tests.Features.SaleTypeDiscounts
{
    public class SaleTypeDiscountsTests
    {
        private sealed class FakeRepository(params SaleTypeDiscount[] rows) : ISaleTypeDiscountRepository
        {
            public int Calls { get; private set; }

            public Task<IReadOnlyList<SaleTypeDiscount>> GetAllAsync(CancellationToken cancellationToken = default)
            {
                Calls++;
                return Task.FromResult<IReadOnlyList<SaleTypeDiscount>>(rows);
            }
        }

        private static GetSaleTypeDiscountsHandler Handler(ISaleTypeDiscountRepository repository) =>
            new(repository, NullLogger<GetSaleTypeDiscountsHandler>.Instance);

        [Fact]
        public async Task Returns_every_row_with_every_column()
        {
            var repository = new FakeRepository(
                SaleTypeDiscount.Reconstitute(1, 7, "نقدی", 6, 1, false),
                SaleTypeDiscount.Reconstitute(2, 84, "اعتباری", 22, 0, true));

            var result = await Handler(repository).Handle();

            Assert.Equal(2, result.Items.Count);
            Assert.Equal(new SaleTypeDiscountListItem(1, 7, "نقدی", 6, 1, false), result.Items[0]);
            Assert.Equal(new SaleTypeDiscountListItem(2, 84, "اعتباری", 22, 0, true), result.Items[1]);
        }

        [Fact]
        public async Task Empty_table_returns_an_empty_list_not_null()
        {
            var result = await Handler(new FakeRepository()).Handle();

            Assert.NotNull(result.Items);
            Assert.Empty(result.Items);
        }

        [Fact]
        public async Task Nullable_columns_are_passed_through_as_null()
        {
            var repository = new FakeRepository(SaleTypeDiscount.Reconstitute(3, 9, null, null, null, null));

            var item = Assert.Single((await Handler(repository).Handle()).Items);

            Assert.Null(item.Title);
            Assert.Null(item.Discount);
            Assert.Null(item.IsCash);
            Assert.Null(item.BlackList);
        }

        [Fact]
        public async Task Reads_the_repository_exactly_once()
        {
            var repository = new FakeRepository();

            await Handler(repository).Handle();

            Assert.Equal(1, repository.Calls);
        }

        [Fact]
        public async Task Cancellation_is_forwarded_to_the_repository()
        {
            var repository = new CancellationAwareRepository();
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            await Assert.ThrowsAsync<OperationCanceledException>(() => Handler(repository).Handle(cts.Token));
        }

        private sealed class CancellationAwareRepository : ISaleTypeDiscountRepository
        {
            public Task<IReadOnlyList<SaleTypeDiscount>> GetAllAsync(CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Task.FromResult<IReadOnlyList<SaleTypeDiscount>>([]);
            }
        }

        [Fact]
        public void Json_uses_camel_case_names_for_all_six_columns()
        {
            var json = JsonSerializer.Serialize(
                new GetSaleTypeDiscountsResult([new SaleTypeDiscountListItem(1, 7, "t", 6, 1, false)]),
                new JsonSerializerOptions(JsonSerializerDefaults.Web));

            using var doc = JsonDocument.Parse(json);
            var item = doc.RootElement.GetProperty("items")[0];

            Assert.Equal(1, item.GetProperty("id").GetInt32());
            Assert.Equal(7, item.GetProperty("saleTypeRef").GetInt32());
            Assert.Equal("t", item.GetProperty("title").GetString());
            Assert.Equal(6, item.GetProperty("discount").GetDecimal());
            Assert.Equal(1, item.GetProperty("isCash").GetInt32());
            Assert.False(item.GetProperty("blackList").GetBoolean());
        }

        [Fact]
        public async Task Controller_returns_200_with_the_handler_result()
        {
            var handler = Handler(new FakeRepository(SaleTypeDiscount.Reconstitute(1, 7, "t", 6, 1, false)));
            var controller = new SaleTypeDiscountsController(handler);

            var action = await controller.List(CancellationToken.None);

            var ok = Assert.IsType<OkObjectResult>(action.Result);
            var body = Assert.IsType<GetSaleTypeDiscountsResult>(ok.Value);
            Assert.Single(body.Items);
        }

        [Fact]
        public void Endpoint_is_POST_api_sale_type_discounts_list_and_requires_login()
        {
            var type = typeof(SaleTypeDiscountsController);
            var route = type.GetCustomAttribute<RouteAttribute>()!;
            var action = type.GetMethod(nameof(SaleTypeDiscountsController.List))!;
            var post = action.GetCustomAttribute<HttpPostAttribute>()!;

            Assert.Equal("api/sale-type-discounts", route.Template);
            Assert.Equal("list", post.Template);
            Assert.NotNull(type.GetCustomAttribute<AuthorizeAttribute>());
            Assert.Null(action.GetCustomAttribute<AllowAnonymousAttribute>());
            Assert.Null(action.GetCustomAttribute<HttpGetAttribute>());
        }

        [Fact]
        public void Repository_reads_the_whole_table_ordered_by_sale_type()
        {
            var sql = (string)typeof(SaleTypeDiscountRepository)
                .GetField("SqlSelectAll", BindingFlags.NonPublic | BindingFlags.Static)!.GetRawConstantValue()!;

            Assert.Contains("FROM dbo.SaleTypeDiscount", sql);
            Assert.Contains("SELECT Id, SaleTypeRef, Title, Discount, IsCash, BlackList", sql);
            Assert.DoesNotContain("WHERE", sql);
            Assert.Contains("ORDER BY SaleTypeRef, Id", sql);
        }
    }
}
