using System.Net;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using SalesManagement.API.Controllers;
using SalesManagement.Application.Common.Interfaces;
using SalesManagement.Application.Features.Customers.CreateCustomer;
using SalesManagement.Domain.Common.Exceptions;
using SalesManagement.Domain.Features.Auth.PartyLogin;
using SalesManagement.Domain.Features.Customers;
using SalesManagement.Tests.Features.Payments;

namespace SalesManagement.Tests.Features.Customers
{
    public class CustomerLicenseFileRulesTests
    {
        private const long FiveMb = 5L * 1024 * 1024;

        [Theory]
        [InlineData(".jpg")]
        [InlineData(".jpeg")]
        [InlineData(".png")]
        [InlineData(".webp")]
        [InlineData(".gif")]
        [InlineData(".pdf")]
        [InlineData(".PDF")]
        [InlineData(".Jpg")]
        [InlineData("pdf")]
        [InlineData("PNG")]
        [InlineData(" .pdf ")]
        public void Allowed_types_are_accepted_in_any_case_with_or_without_a_dot(string extension)
        {
            CustomerLicenseFileRules.Validate(extension, 1024);
        }

        [Theory]
        [InlineData(".html")]
        [InlineData(".htm")]
        [InlineData(".svg")]
        [InlineData(".exe")]
        [InlineData(".dll")]
        [InlineData(".bat")]
        [InlineData(".ps1")]
        [InlineData(".js")]
        [InlineData(".php")]
        [InlineData(".aspx")]
        [InlineData(".config")]
        [InlineData(".zip")]
        [InlineData(".docx")]
        [InlineData(".xhtml")]
        [InlineData(".jpg.exe")]
        [InlineData(".pdf.html")]
        [InlineData(".")]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Everything_else_is_rejected_including_a_missing_extension(string? extension)
        {
            var ex = Assert.Throws<DomainException>(() => CustomerLicenseFileRules.Validate(extension, 1024));

            Assert.Equal("فرمت فایل مدرک مجاز نیست (فقط jpg, jpeg, png, webp, gif, pdf)", ex.Message);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Empty_files_are_rejected(long length)
        {
            var ex = Assert.Throws<DomainException>(() => CustomerLicenseFileRules.Validate(".pdf", length));

            Assert.Equal("فایل مدرک خالی است", ex.Message);
        }

        [Fact]
        public void Exactly_five_megabytes_is_accepted()
        {
            CustomerLicenseFileRules.Validate(".pdf", FiveMb);
        }

        [Theory]
        [InlineData(5L * 1024 * 1024 + 1)]
        [InlineData(6L * 1024 * 1024)]
        [InlineData(500_000_000)]
        public void Above_five_megabytes_is_rejected(long length)
        {
            var ex = Assert.Throws<DomainException>(() => CustomerLicenseFileRules.Validate(".png", length));

            Assert.Equal("حجم فایل مدرک نباید بیشتر از ۵ مگابایت باشد", ex.Message);
        }

        [Fact]
        public void The_limit_is_five_megabytes()
        {
            Assert.Equal(FiveMb, CustomerLicenseFileRules.MaxBytes);
        }

        [Theory]
        [InlineData("PDF", ".pdf")]
        [InlineData(".PDF", ".pdf")]
        [InlineData(" jpg ", ".jpg")]
        [InlineData(".png", ".png")]
        [InlineData(null, "")]
        [InlineData("", "")]
        public void Extensions_are_normalized_to_lowercase_with_a_leading_dot(string? input, string expected)
        {
            Assert.Equal(expected, CustomerLicenseFileRules.NormalizeExtension(input));
        }

        [Fact]
        public void Every_allowed_extension_fits_the_five_character_database_column()
        {
            foreach (var ext in new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif", ".pdf" })
                Assert.True(ext.Length <= 5);
        }

        [Fact]
        public void Type_is_checked_before_size_and_emptiness_before_type()
        {
            Assert.Equal("فایل مدرک خالی است",
                Assert.Throws<DomainException>(() => CustomerLicenseFileRules.Validate(".exe", 0)).Message);
            Assert.Equal("فرمت فایل مدرک مجاز نیست (فقط jpg, jpeg, png, webp, gif, pdf)",
                Assert.Throws<DomainException>(() => CustomerLicenseFileRules.Validate(".exe", FiveMb * 10)).Message);
        }
    }

    public class CreateCustomerLicenseHandlerTests
    {
        private sealed class RecordingStorage : IFileStorage
        {
            public List<(string Folder, string Extension, long Length)> Saved { get; } = [];
            public List<(string Folder, string FileName)> Deleted { get; } = [];

            public Task<string> SaveAsync(Stream content, string folder, string extension, CancellationToken ct = default)
            {
                Saved.Add((folder, extension, content.Length));
                return Task.FromResult($"file{extension}");
            }

            public Task DeleteAsync(string folder, string fileName, CancellationToken ct = default)
            {
                Deleted.Add((folder, fileName));
                return Task.CompletedTask;
            }
        }

        private sealed class FakeCustomerRepository : ICustomerRepository
        {
            public int CreateCalls { get; private set; }
            public int UpdateCalls { get; private set; }
            public NewCustomer? LastCustomer { get; private set; }
            public Exception? Failure { get; set; }

            public Task<int> CreateAsync(NewCustomer customer, string? visitorCode, CancellationToken ct = default)
            {
                CreateCalls++;
                LastCustomer = customer;
                if (Failure is not null) throw Failure;
                return Task.FromResult(42);
            }

            public Task<IReadOnlyList<string>> UpdateAsync(NewCustomer customer, string? visitorCode, CancellationToken ct = default)
            {
                UpdateCalls++;
                LastCustomer = customer;
                if (Failure is not null) throw Failure;
                return Task.FromResult<IReadOnlyList<string>>([]);
            }
        }

        private sealed class SizedStream(long length) : Stream
        {
            public override bool CanRead => true;
            public override bool CanSeek => true;
            public override bool CanWrite => false;
            public override long Length => length;
            public override long Position { get; set; }
            public override void Flush() { }
            public override int Read(byte[] buffer, int offset, int count) => 0;
            public override long Seek(long offset, SeekOrigin origin) => 0;
            public override void SetLength(long value) { }
            public override void Write(byte[] buffer, int offset, int count) { }
        }

        private readonly RecordingStorage _storage = new();
        private readonly FakeCustomerRepository _repository = new();

        private CreateCustomerHandler CreateHandler() =>
            new(_repository, _storage, NullLogger<CreateCustomerHandler>.Instance);

        private static CreateCustomerCommand Command(
            Stream? file, string? extension, int? partyId = null, string? nationalCode = "0499370899") => new(
            partyId, null, "Ali", "Rezaei", nationalCode, "02100000000", "09120000000", "09120000000",
            "Some Address", "1234567890", 1, null, null, null, null, null, null, null, null, file, extension);

        [Theory]
        [InlineData(".pdf", ".pdf")]
        [InlineData(".PDF", ".pdf")]
        [InlineData("pdf", ".pdf")]
        [InlineData(".jpg", ".jpg")]
        [InlineData(".JPEG", ".jpeg")]
        [InlineData(".png", ".png")]
        [InlineData(".webp", ".webp")]
        [InlineData(".gif", ".gif")]
        public async Task Allowed_files_are_stored_in_the_party_folder_with_a_normalized_extension(string given, string stored)
        {
            await CreateHandler().Handle(Command(new SizedStream(2048), given));

            Assert.Equal(("Party", stored, 2048), Assert.Single(_storage.Saved));
            Assert.Equal(1, _repository.CreateCalls);
            Assert.Equal($"file{stored}", _repository.LastCustomer!.LicenseFileName);
        }

        [Theory]
        [InlineData(".html", 1024)]
        [InlineData(".exe", 1024)]
        [InlineData(".svg", 1024)]
        [InlineData(".php", 1024)]
        [InlineData(".jpg.exe", 1024)]
        [InlineData("", 1024)]
        [InlineData(null, 1024)]
        [InlineData(".pdf", 0)]
        [InlineData(".pdf", 5L * 1024 * 1024 + 1)]
        [InlineData(".jpg", 500_000_000)]
        public async Task Invalid_files_are_rejected_before_anything_is_stored_or_created(string? extension, long length)
        {
            await Assert.ThrowsAsync<DomainException>(() =>
                CreateHandler().Handle(Command(new SizedStream(length), extension)));

            Assert.Empty(_storage.Saved);
            Assert.Equal(0, _repository.CreateCalls);
            Assert.Equal(0, _repository.UpdateCalls);
        }

        [Fact]
        public async Task A_file_of_exactly_five_megabytes_is_accepted()
        {
            await CreateHandler().Handle(Command(new SizedStream(5L * 1024 * 1024), ".pdf"));

            Assert.Single(_storage.Saved);
        }

        [Fact]
        public async Task Registration_without_a_file_still_works()
        {
            var result = await CreateHandler().Handle(Command(null, null));

            Assert.Equal(42, result.PartyId);
            Assert.Empty(_storage.Saved);
            Assert.Null(_repository.LastCustomer!.LicenseFileName);
        }

        [Fact]
        public async Task Updating_an_existing_customer_applies_the_same_rules()
        {
            await Assert.ThrowsAsync<DomainException>(() =>
                CreateHandler().Handle(Command(new SizedStream(10), ".html", partyId: 55)));

            Assert.Empty(_storage.Saved);
            Assert.Equal(0, _repository.UpdateCalls);

            await CreateHandler().Handle(Command(new SizedStream(10), ".pdf", partyId: 55));

            Assert.Equal(1, _repository.UpdateCalls);
            Assert.Single(_storage.Saved);
        }

        [Fact]
        public async Task Invalid_customer_data_fails_before_the_file_is_looked_at()
        {
            await Assert.ThrowsAsync<DomainException>(() =>
                CreateHandler().Handle(Command(new SizedStream(10), ".pdf", nationalCode: "123")));

            Assert.Empty(_storage.Saved);
        }

        [Fact]
        public async Task A_stored_file_is_removed_again_when_saving_the_customer_fails()
        {
            _repository.Failure = new InvalidOperationException("db down");

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                CreateHandler().Handle(Command(new SizedStream(10), ".pdf")));

            Assert.Equal(("Party", "file.pdf"), Assert.Single(_storage.Deleted));
        }
    }

    public class CustomersApiFactory : WebApplicationFactory<Program>
    {
        static CustomersApiFactory() => TestSettings.Apply();

        internal sealed class Recording : IFileStorage
        {
            public List<(string Folder, string Extension, long Length)> Saved { get; } = [];
            public List<(string Folder, string FileName)> Deleted { get; } = [];

            public Task<string> SaveAsync(Stream content, string folder, string extension, CancellationToken ct = default)
            {
                Saved.Add((folder, extension, content.Length));
                return Task.FromResult($"stored{extension}");
            }

            public Task DeleteAsync(string folder, string fileName, CancellationToken ct = default)
            {
                Deleted.Add((folder, fileName));
                return Task.CompletedTask;
            }
        }

        internal sealed class Repo : ICustomerRepository
        {
            public List<NewCustomer> Created { get; } = [];

            public Task<int> CreateAsync(NewCustomer customer, string? visitorCode, CancellationToken ct = default)
            {
                Created.Add(customer);
                return Task.FromResult(4242);
            }

            public Task<IReadOnlyList<string>> UpdateAsync(NewCustomer customer, string? visitorCode, CancellationToken ct = default) =>
                Task.FromResult<IReadOnlyList<string>>([]);
        }

        internal Recording Storage { get; } = new();
        internal Repo Customers { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureTestServices(services =>
            {
                foreach (var hosted in services.Where(d => d.ServiceType == typeof(IHostedService)).ToList())
                    services.Remove(hosted);

                foreach (var d in services.Where(d =>
                             d.ServiceType == typeof(IFileStorage) || d.ServiceType == typeof(ICustomerRepository)
                             || d.ServiceType == typeof(IRevokedTokenStore)).ToList())
                    services.Remove(d);

                services.AddSingleton<IFileStorage>(Storage);
                services.AddSingleton<ICustomerRepository>(Customers);
                services.AddSingleton<IRevokedTokenStore, InMemoryRevokedTokenStore>();
            });
        }

        internal void Reset()
        {
            Storage.Saved.Clear();
            Storage.Deleted.Clear();
            Customers.Created.Clear();
        }
    }

    [CollectionDefinition(Name)]
    public class CustomersApiCollection : ICollectionFixture<CustomersApiFactory>
    {
        public const string Name = "CustomersApi";
    }

    [Collection(CustomersApiCollection.Name)]
    public class CustomersApiTests
    {
        private readonly CustomersApiFactory _factory;

        public CustomersApiTests(CustomersApiFactory factory)
        {
            _factory = factory;
            _factory.Reset();
        }

        private static MultipartFormDataContent Form(string? fileName = null, byte[]? file = null, string nationalCode = "0499370899")
        {
            var form = new MultipartFormDataContent
            {
                { new StringContent("Ali"), "FirstName" },
                { new StringContent("Rezaei"), "LastName" },
                { new StringContent(nationalCode), "NationalCode" },
                { new StringContent("02100000000"), "LandlinePhone" },
                { new StringContent("09120000000"), "MobilePhone" },
                { new StringContent("09120000000"), "MessengerPhone" },
                { new StringContent("Some Address"), "Address" },
                { new StringContent("1234567890"), "ZipCode" },
                { new StringContent("1"), "LocationRef" },
                { new StringContent("36.5,52.6"), "Geolocation" },
            };

            if (fileName is not null)
                form.Add(new ByteArrayContent(file ?? new byte[100]), "LicenseImage", fileName);

            return form;
        }

        private static async Task<string?> Detail(HttpResponseMessage response)
        {
            var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
            return json.TryGetProperty("detail", out var d) ? d.GetString() : null;
        }

        [Fact]
        public async Task Anonymous_registration_without_a_file_succeeds()
        {
            using var client = _factory.CreateClient();

            var response = await client.PostAsync("/api/customers/create", Form());

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Single(_factory.Customers.Created);
            Assert.Empty(_factory.Storage.Saved);
        }

        [Theory]
        [InlineData("license.pdf", ".pdf")]
        [InlineData("license.PDF", ".pdf")]
        [InlineData("scan.jpg", ".jpg")]
        [InlineData("scan.jpeg", ".jpeg")]
        [InlineData("scan.png", ".png")]
        [InlineData("scan.webp", ".webp")]
        [InlineData("scan.gif", ".gif")]
        public async Task Allowed_documents_are_accepted_and_stored(string fileName, string storedExtension)
        {
            using var client = _factory.CreateClient();

            var response = await client.PostAsync("/api/customers/create", Form(fileName));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(("Party", storedExtension, 100), Assert.Single(_factory.Storage.Saved));
            Assert.Equal($"stored{storedExtension}", Assert.Single(_factory.Customers.Created).LicenseFileName);
        }

        [Theory]
        [InlineData("page.html")]
        [InlineData("vector.svg")]
        [InlineData("run.exe")]
        [InlineData("shell.php")]
        [InlineData("app.aspx")]
        [InlineData("photo.jpg.exe")]
        [InlineData("noextension")]
        public async Task Disallowed_documents_are_400_and_nothing_is_stored_or_created(string fileName)
        {
            using var client = _factory.CreateClient();

            var response = await client.PostAsync("/api/customers/create", Form(fileName));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("فرمت فایل مدرک مجاز نیست (فقط jpg, jpeg, png, webp, gif, pdf)", await Detail(response));
            Assert.Empty(_factory.Storage.Saved);
            Assert.Empty(_factory.Customers.Created);
        }

        [Fact]
        public async Task An_empty_file_is_400()
        {
            using var client = _factory.CreateClient();

            var response = await client.PostAsync("/api/customers/create", Form("empty.pdf", []));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Empty(_factory.Customers.Created);
        }

        [Fact]
        public async Task A_document_just_over_five_megabytes_is_400_with_the_size_message()
        {
            using var client = _factory.CreateClient();

            var response = await client.PostAsync(
                "/api/customers/create", Form("big.pdf", new byte[5 * 1024 * 1024 + 1]));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("حجم فایل مدرک نباید بیشتر از ۵ مگابایت باشد", await Detail(response));
            Assert.Empty(_factory.Storage.Saved);
        }

        [Fact]
        public async Task A_document_of_exactly_five_megabytes_is_accepted()
        {
            using var client = _factory.CreateClient();

            var response = await client.PostAsync(
                "/api/customers/create", Form("max.pdf", new byte[5 * 1024 * 1024]));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Single(_factory.Storage.Saved);
        }

        [Fact]
        public async Task A_request_body_above_ten_megabytes_is_rejected_and_nothing_is_stored()
        {
            using var client = _factory.CreateClient();

            var response = await client.PostAsync(
                "/api/customers/create", Form("huge.pdf", new byte[11 * 1024 * 1024]));

            Assert.Contains(response.StatusCode, new[] { HttpStatusCode.BadRequest, HttpStatusCode.RequestEntityTooLarge });
            Assert.Empty(_factory.Storage.Saved);
            Assert.Empty(_factory.Customers.Created);
        }

        [Fact]
        public async Task Invalid_customer_data_with_a_valid_file_stores_nothing()
        {
            using var client = _factory.CreateClient();

            var response = await client.PostAsync(
                "/api/customers/create", Form("license.pdf", nationalCode: "123"));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Empty(_factory.Storage.Saved);
            Assert.Empty(_factory.Customers.Created);
        }
    }

    public class CustomersControllerAttributeTests
    {
        private static MethodInfo Create => typeof(CustomersController).GetMethod(nameof(CustomersController.Create))!;

        [Fact]
        public void Registration_stays_anonymous_multipart_and_post()
        {
            Assert.NotNull(Create.GetCustomAttribute<AllowAnonymousAttribute>());
            Assert.NotNull(Create.GetCustomAttribute<HttpPostAttribute>());
            Assert.Equal("multipart/form-data", Create.GetCustomAttribute<ConsumesAttribute>()!.ContentTypes.Single());
        }

        [Fact]
        public void The_whole_request_is_capped_at_ten_megabytes()
        {
            var limit = 10L * 1024 * 1024;

            Assert.Equal(limit, Create.GetCustomAttribute<RequestSizeLimitAttribute>()!.GetType()
                .GetField("_bytes", BindingFlags.NonPublic | BindingFlags.Instance)!
                .GetValue(Create.GetCustomAttribute<RequestSizeLimitAttribute>()));
            Assert.Equal(limit, Create.GetCustomAttribute<RequestFormLimitsAttribute>()!.MultipartBodyLengthLimit);
        }
    }
}
