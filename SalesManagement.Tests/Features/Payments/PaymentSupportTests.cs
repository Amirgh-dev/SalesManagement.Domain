using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using SalesManagement.Domain.Enums;
using SalesManagement.Domain.Features.Payments;
using SalesManagement.Infrastructure.Payments;
using SalesManagement.Persistence.Features.Payments;

namespace SalesManagement.Tests.Features.Payments
{
    public class PaymentCallbackUrlProviderTests
    {
        private static PaymentCallbackUrlProvider Create(string baseUrl) =>
            new(Options.Create(new ZarinPalSettings { CallbackBaseUrl = baseUrl }));

        [Fact]
        public void Builds_the_callback_url_with_the_payment_id()
        {
            var id = Guid.Parse("11111111-2222-3333-4444-555555555555");

            var url = Create("https://api.example").Build(id);

            Assert.Equal("https://api.example/api/payments/callback?paymentId=11111111-2222-3333-4444-555555555555", url);
        }

        [Theory]
        [InlineData("https://api.example/")]
        [InlineData("https://api.example//")]
        public void Trailing_slashes_on_the_base_url_are_removed(string baseUrl)
        {
            var url = Create(baseUrl).Build(Guid.Empty);

            Assert.Equal($"https://api.example/api/payments/callback?paymentId={Guid.Empty}", url);
        }

        [Fact]
        public void Base_url_with_a_port_and_path_is_preserved()
        {
            var url = Create("http://localhost:5280/base").Build(Guid.Empty);

            Assert.StartsWith("http://localhost:5280/base/api/payments/callback?paymentId=", url);
        }

        [Fact]
        public void Different_payment_ids_produce_different_urls()
        {
            var provider = Create("https://api.example");

            Assert.NotEqual(provider.Build(Guid.NewGuid()), provider.Build(Guid.NewGuid()));
        }

        [Fact]
        public void Payment_id_is_written_in_the_default_dashed_format()
        {
            var id = Guid.NewGuid();

            Assert.EndsWith($"paymentId={id:D}", Create("https://api.example").Build(id));
        }
    }

    public class ZarinPalSettingsTests
    {
        [Fact]
        public void Section_name_is_ZarinPal()
        {
            Assert.Equal("ZarinPal", ZarinPalSettings.SectionName);
        }

        [Fact]
        public void Defaults_point_at_the_production_gateway_and_leave_secrets_empty()
        {
            var settings = new ZarinPalSettings();

            Assert.Equal("https://payment.zarinpal.com", settings.BaseUrl);
            Assert.Equal(string.Empty, settings.MerchantId);
            Assert.Equal(string.Empty, settings.CallbackBaseUrl);
            Assert.Equal(string.Empty, settings.FrontendResultUrl);
        }
    }

    public class PaymentRepositoryMappingTests
    {
        private static readonly Type RepositoryType = typeof(PaymentRepository);

        private static string Sql(string name) =>
            (string)RepositoryType.GetField(name, BindingFlags.NonPublic | BindingFlags.Static)!.GetRawConstantValue()!;

        private static HashSet<string> Parameters(string sql) =>
            Regex.Matches(sql, @"@(\w+)").Select(m => m.Groups[1].Value).ToHashSet();

        private static Payment FullPayment() =>
            Payment.Create(7823, "Invoice", 100, "ZarinPal", 248_200_000, "desc", "web", "10.0.0.1")
                .MarkSentToGateway("AUTH-1", 1000)
                .MarkVerified(900001, "603799******1234", "HASH")
                .MarkApproved(12);

        private static IDictionary<string, object?> Args(Payment payment)
        {
            var args = RepositoryType.GetMethod("ToArgs", BindingFlags.NonPublic | BindingFlags.Static)!
                .Invoke(null, [payment])!;
            return args.GetType().GetProperties().ToDictionary(p => p.Name, p => p.GetValue(args));
        }

        private static readonly string[] AllColumns =
        [
            "Id", "PartyRef", "TableName", "TargetRef", "Gateway", "Amount", "Description", "Authority", "State",
            "IsPay", "IsOk", "Modifier", "LastModificationDate", "RefId", "TrackingCode", "CardPan", "CardHash",
            "Fee", "ErrorCode", "ErrorMessage", "Channel", "IpAddress", "CreationDate", "VerifiedDate",
            "LastModifiedDate",
        ];

        [Fact]
        public void Table_has_exactly_the_twenty_five_expected_columns()
        {
            Assert.Equal(25, AllColumns.Length);
            Assert.Equal(AllColumns.Order(), typeof(Payment).GetProperties().Select(p => p.Name).Order());
        }

        [Fact]
        public void Insert_supplies_a_parameter_for_every_column_in_the_same_order()
        {
            var sql = Sql("SqlInsert");
            var columns = Regex.Match(sql, @"INSERT INTO dbo\.Payments\s*\((.*?)\)\s*VALUES", RegexOptions.Singleline)
                .Groups[1].Value.Split(',').Select(c => c.Trim()).ToArray();
            var values = Regex.Match(sql, @"VALUES\s*\((.*)\)\s*;", RegexOptions.Singleline)
                .Groups[1].Value.Split(',').Select(v => v.Trim().TrimStart('@')).ToArray();

            Assert.Equal(AllColumns, columns);
            Assert.Equal(columns, values);
        }

        [Fact]
        public void Insert_parameters_are_all_provided_by_the_argument_object()
        {
            var provided = Args(FullPayment()).Keys.ToHashSet();

            Assert.Equal(Parameters(Sql("SqlInsert")).Order(), provided.Order());
        }

        [Fact]
        public void Update_only_touches_the_mutable_columns()
        {
            var expected = new HashSet<string>
            {
                "Id", "Authority", "State", "IsPay", "IsOk", "Modifier", "LastModificationDate", "RefId",
                "CardPan", "CardHash", "Fee", "ErrorCode", "ErrorMessage", "VerifiedDate", "LastModifiedDate",
            };

            var parameters = Parameters(Sql("SqlUpdate"));

            Assert.Equal(expected.Order(), parameters.Order());
        }

        [Theory]
        [InlineData("PartyRef")]
        [InlineData("TableName")]
        [InlineData("TargetRef")]
        [InlineData("Gateway")]
        [InlineData("Amount")]
        [InlineData("Description")]
        [InlineData("TrackingCode")]
        [InlineData("Channel")]
        [InlineData("IpAddress")]
        [InlineData("CreationDate")]
        public void Update_never_changes_immutable_columns(string column)
        {
            Assert.DoesNotContain(column, Parameters(Sql("SqlUpdate")));
        }

        [Fact]
        public void Update_is_scoped_to_a_single_row_by_id()
        {
            Assert.Contains("WHERE Id = @Id", Sql("SqlUpdate"));
        }

        [Fact]
        public void Update_parameters_are_provided_by_the_argument_object()
        {
            var provided = Args(FullPayment()).Keys.ToHashSet();

            Assert.All(Parameters(Sql("SqlUpdate")), p => Assert.Contains(p, provided));
        }

        [Fact]
        public void Selects_read_every_column_and_filter_by_a_single_key()
        {
            var byId = Sql("SqlSelectById");
            var byAuthority = Sql("SqlSelectByAuthority");

            foreach (var column in AllColumns)
            {
                Assert.Contains(column, byId);
                Assert.Contains(column, byAuthority);
            }

            Assert.Contains("WHERE Id = @Id", byId);
            Assert.Contains("WHERE Authority = @Authority", byAuthority);
            Assert.DoesNotContain("SELECT *", byId);
        }

        [Fact]
        public void Exists_verified_query_filters_by_party_table_target_and_state()
        {
            var sql = Sql("SqlExistsVerified");

            Assert.Equal(new[] { "PartyRef", "State", "TableName", "TargetRef" }, Parameters(sql).Order());
        }

        [Fact]
        public void Argument_object_stores_state_as_a_byte_matching_the_tinyint_column()
        {
            var args = Args(FullPayment());

            Assert.IsType<byte>(args["State"]);
            Assert.Equal((byte)PaymentState.Verified, args["State"]);
        }

        [Fact]
        public void Argument_object_carries_the_payment_values()
        {
            var payment = FullPayment();

            var args = Args(payment);

            Assert.Equal(payment.Id, args["Id"]);
            Assert.Equal(payment.PartyRef, args["PartyRef"]);
            Assert.Equal(payment.Amount, args["Amount"]);
            Assert.Equal(payment.Authority, args["Authority"]);
            Assert.Equal(payment.RefId, args["RefId"]);
            Assert.Equal(payment.IsPay, args["IsPay"]);
            Assert.Equal(payment.IsOk, args["IsOk"]);
            Assert.Equal(payment.Modifier, args["Modifier"]);
            Assert.Equal(payment.TrackingCode, args["TrackingCode"]);
        }

        private static object NewRow(Payment payment)
        {
            var rowType = RepositoryType.GetNestedType("PaymentRow", BindingFlags.NonPublic)!;
            var row = Activator.CreateInstance(rowType, nonPublic: true)!;
            foreach (var property in rowType.GetProperties().Where(p => p.CanWrite))
            {
                var source = typeof(Payment).GetProperty(property.Name)!;
                var value = source.GetValue(payment);
                if (property.Name == "State") value = (byte)(PaymentState)value!;
                property.SetValue(row, value);
            }

            return row;
        }

        private static Payment ToDomain(object row) =>
            (Payment)row.GetType().GetMethod("ToDomain")!.Invoke(row, null)!;

        [Fact]
        public void Row_has_a_settable_property_for_every_column()
        {
            var rowType = RepositoryType.GetNestedType("PaymentRow", BindingFlags.NonPublic)!;

            Assert.Equal(AllColumns.Order(), rowType.GetProperties().Select(p => p.Name).Order());
        }

        [Fact]
        public void Row_round_trips_a_fully_populated_payment()
        {
            var original = FullPayment();

            var restored = ToDomain(NewRow(original));

            Assert.Equal(original, restored);
        }

        [Fact]
        public void Row_round_trips_a_freshly_created_payment()
        {
            var original = Payment.Create(1, null, null, "ZarinPal", 1, "d", "web", "1.1.1.1");

            Assert.Equal(original, ToDomain(NewRow(original)));
        }

        [Theory]
        [InlineData(PaymentState.Created)]
        [InlineData(PaymentState.SentToGateway)]
        [InlineData(PaymentState.RequestFailed)]
        [InlineData(PaymentState.CanceledByUser)]
        [InlineData(PaymentState.Verified)]
        [InlineData(PaymentState.VerifyFailed)]
        public void Row_maps_every_state_value_back_to_the_enum(PaymentState state)
        {
            var created = Payment.Create(1, null, null, "ZarinPal", 1, "d", "web", "1.1.1.1");
            var payment = state switch
            {
                PaymentState.SentToGateway => created.MarkSentToGateway("A", null),
                PaymentState.RequestFailed => created.MarkRequestFailed("1", "x"),
                PaymentState.CanceledByUser => created.MarkSentToGateway("A", null).MarkCanceledByUser(),
                PaymentState.Verified => created.MarkSentToGateway("A", null).MarkVerified(1, null, null),
                PaymentState.VerifyFailed => created.MarkSentToGateway("A", null).MarkVerifyFailed("1", "x"),
                _ => created,
            };

            Assert.Equal(state, ToDomain(NewRow(payment)).State);
        }

        [Fact]
        public void Row_with_null_party_ref_maps_to_zero()
        {
            var original = Payment.Create(5, null, null, "ZarinPal", 1, "d", "web", "1.1.1.1");
            var row = NewRow(original);
            row.GetType().GetProperty("PartyRef")!.SetValue(row, null);

            Assert.Equal(0, ToDomain(row).PartyRef);
        }
    }
}
