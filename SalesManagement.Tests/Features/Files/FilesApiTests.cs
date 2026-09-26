using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SalesManagement.API.Controllers;
using SalesManagement.API.Features.Files;
using SalesManagement.Application.Common.Interfaces;
using SalesManagement.Application.Features.Files.DeleteFile;
using SalesManagement.Domain.Features.Auth.PartyLogin;
using SalesManagement.Domain.Features.Files;
using SalesManagement.Tests.Features.Payments;

namespace SalesManagement.Tests.Features.Files
{
    public class FilesApiFactory : WebApplicationFactory<Program>
    {
        static FilesApiFactory() => TestSettings.Apply();

        public FakeFileSource Posts { get; } = new("PostFile");
        public FakeFileSource Parties { get; } = new("PartyFile");
        public FakeFileStorage Storage { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureTestServices(services =>
            {
                foreach (var hosted in services.Where(d => d.ServiceType == typeof(IHostedService)).ToList())
                    services.Remove(hosted);

                foreach (var d in services.Where(d =>
                             d.ServiceType == typeof(IFileSource) || d.ServiceType == typeof(IFileStorage)
                             || d.ServiceType == typeof(IRevokedTokenStore)).ToList())
                    services.Remove(d);

                services.AddSingleton<IFileSource>(Posts);
                services.AddSingleton<IFileSource>(Parties);
                services.AddSingleton<IFileStorage>(Storage);
                services.AddSingleton<IRevokedTokenStore, InMemoryRevokedTokenStore>();
            });
        }

        public void Reset()
        {
            Posts.Clear();
            Parties.Clear();
            Storage.Clear();
        }

        public HttpClient CreateApiClient(int? partyId = null)
        {
            var client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            if (partyId is not null)
            {
                using var scope = Services.CreateScope();
                var token = scope.ServiceProvider.GetRequiredService<ITokenGenerator>()
                    .GenerateAccessToken(partyId.Value, "09000000000");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            return client;
        }
    }

    [CollectionDefinition(Name)]
    public class FilesApiCollection : ICollectionFixture<FilesApiFactory>
    {
        public const string Name = "FilesApi";
    }

    [Collection(FilesApiCollection.Name)]
    public class FilesApiTests
    {
        private const int Party = 7823;

        private readonly FilesApiFactory _factory;

        public FilesApiTests(FilesApiFactory factory)
        {
            _factory = factory;
            _factory.Reset();
        }

        private static StringContent Json(string json) => new(json, Encoding.UTF8, "application/json");

        private static StringContent IdBody(Guid id) => Json($$"""{"id":"{{id}}"}""");

        private static async Task<string?> Detail(HttpResponseMessage response)
        {
            var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
            return json.TryGetProperty("detail", out var d) ? d.GetString() : null;
        }

        [Fact]
        public async Task Deleting_an_owned_post_file_returns_204_and_removes_the_disk_file()
        {
            var id = Guid.NewGuid();
            _factory.Posts.Owns(id, Party, new StoredFile("PostFile", "p1.png"));
            using var client = _factory.CreateApiClient(Party);

            var response = await client.PostAsync("/api/files/delete", IdBody(id));

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(("PostFile", "p1.png"), Assert.Single(_factory.Storage.Deleted));
        }

        [Theory]
        [InlineData("/api/files/delete")]
        [InlineData("/api/Files/Delete")]
        [InlineData("/api/FILES/DELETE")]
        public async Task The_route_is_case_insensitive(string route)
        {
            var id = Guid.NewGuid();
            _factory.Posts.Owns(id, Party, new StoredFile("PostFile", "p1.png"));
            using var client = _factory.CreateApiClient(Party);

            var response = await client.PostAsync(route, IdBody(id));

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(("PostFile", "p1.png"), Assert.Single(_factory.Storage.Deleted));
        }

        [Theory]
        [InlineData("/api/file/delete")]
        [InlineData("/api/File/Delete")]
        [InlineData("/api/files")]
        [InlineData("/api/files/remove")]
        public async Task Other_spellings_are_not_routes(string route)
        {
            using var client = _factory.CreateApiClient(Party);

            var response = await client.PostAsync(route, IdBody(Guid.NewGuid()));

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal("مسیر مورد نظر پیدا نشد", await Detail(response));
            Assert.Empty(_factory.Posts.Calls);
        }

        [Fact]
        public async Task Deleting_an_owned_party_file_works_through_the_second_source()
        {
            var id = Guid.NewGuid();
            _factory.Parties.Owns(id, Party, new StoredFile("Profile", "me.jpg"));
            using var client = _factory.CreateApiClient(Party);

            var response = await client.PostAsync("/api/files/delete", IdBody(id));

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(("Profile", "me.jpg"), Assert.Single(_factory.Storage.Deleted));
        }

        [Fact]
        public async Task Party_comes_from_the_token_so_someone_else_cannot_delete_the_file()
        {
            var id = Guid.NewGuid();
            _factory.Posts.Owns(id, partyId: 555, new StoredFile("PostFile", "p1.png"));
            using var client = _factory.CreateApiClient(Party);

            var response = await client.PostAsync("/api/files/delete", IdBody(id));

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal("فایل پیدا نشد", await Detail(response));
            Assert.Empty(_factory.Storage.Deleted);
        }

        [Fact]
        public async Task A_party_id_sent_in_the_body_is_ignored()
        {
            var id = Guid.NewGuid();
            _factory.Posts.Owns(id, partyId: 555, new StoredFile("PostFile", "p1.png"));
            using var client = _factory.CreateApiClient(Party);

            var response = await client.PostAsync("/api/files/delete", Json($$"""{"id":"{{id}}","partyId":555,"partyRef":555}"""));

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal((id, Party), _factory.Posts.Calls.Single());
        }

        [Fact]
        public async Task Unknown_id_is_404()
        {
            using var client = _factory.CreateApiClient(Party);

            var response = await client.PostAsync("/api/files/delete", IdBody(Guid.NewGuid()));

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Without_a_token_the_request_is_401_and_no_source_is_asked()
        {
            using var client = _factory.CreateApiClient();

            var response = await client.PostAsync("/api/files/delete", IdBody(Guid.NewGuid()));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Empty(_factory.Posts.Calls);
            Assert.Empty(_factory.Parties.Calls);
        }

        [Fact]
        public async Task With_a_garbage_token_the_request_is_401()
        {
            using var client = _factory.CreateApiClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "garbage");

            var response = await client.PostAsync("/api/files/delete", IdBody(Guid.NewGuid()));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Theory]
        [InlineData("{}")]
        [InlineData("""{"id":null}""")]
        [InlineData("""{"id":"not-a-guid"}""")]
        [InlineData("""{"id":123}""")]
        [InlineData("""{"id":""}""")]
        [InlineData("{ broken")]
        public async Task Invalid_body_is_400_and_no_source_is_asked(string body)
        {
            using var client = _factory.CreateApiClient(Party);

            var response = await client.PostAsync("/api/files/delete", Json(body));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Empty(_factory.Posts.Calls);
        }

        [Fact]
        public async Task Empty_guid_is_400()
        {
            using var client = _factory.CreateApiClient(Party);

            var response = await client.PostAsync("/api/files/delete", IdBody(Guid.Empty));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("شناسه‌ی فایل نامعتبر است", await Detail(response));
            Assert.Empty(_factory.Posts.Calls);
        }

        [Fact]
        public async Task Empty_body_is_400()
        {
            using var client = _factory.CreateApiClient(Party);

            var response = await client.PostAsync("/api/files/delete", Json(""));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Plain_text_content_type_is_415()
        {
            using var client = _factory.CreateApiClient(Party);

            var response = await client.PostAsync(
                "/api/files/delete", new StringContent("id=1", Encoding.UTF8, "text/plain"));

            Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
        }

        [Theory]
        [InlineData("GET")]
        [InlineData("PUT")]
        [InlineData("DELETE")]
        public async Task Only_post_is_allowed(string method)
        {
            var id = Guid.NewGuid();
            _factory.Posts.Owns(id, Party, new StoredFile("PostFile", "p1.png"));
            using var client = _factory.CreateApiClient(Party);

            var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), "/api/files/delete")
            {
                Content = IdBody(id),
            });

            Assert.Contains(response.StatusCode, new[] { HttpStatusCode.MethodNotAllowed, HttpStatusCode.NotFound });
            Assert.Empty(_factory.Storage.Deleted);
        }

        [Fact]
        public async Task Deleting_twice_is_204_then_404()
        {
            var id = Guid.NewGuid();
            _factory.Posts.Owns(id, Party, new StoredFile("PostFile", "p1.png"));
            using var client = _factory.CreateApiClient(Party);

            var first = await client.PostAsync("/api/files/delete", IdBody(id));
            var second = await client.PostAsync("/api/files/delete", IdBody(id));

            Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, second.StatusCode);
            Assert.Single(_factory.Storage.Deleted);
        }

        [Fact]
        public async Task Source_failure_is_500_and_nothing_is_removed_from_disk()
        {
            _factory.Posts.Failure = new InvalidOperationException("db down");
            using var client = _factory.CreateApiClient(Party);

            var response = await client.PostAsync("/api/files/delete", IdBody(Guid.NewGuid()));

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.Empty(_factory.Storage.Deleted);
        }

        [Fact]
        public async Task Disk_failure_after_the_row_delete_still_returns_204()
        {
            var id = Guid.NewGuid();
            _factory.Posts.Owns(id, Party, new StoredFile("PostFile", "p1.png"));
            _factory.Storage.DeleteFailure = new IOException("locked");
            using var client = _factory.CreateApiClient(Party);

            var response = await client.PostAsync("/api/files/delete", IdBody(id));

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task Unsafe_stored_names_return_204_but_never_touch_the_disk()
        {
            var id = Guid.NewGuid();
            _factory.Posts.Owns(id, Party, new StoredFile("PostFile", "../../appsettings.json"));
            using var client = _factory.CreateApiClient(Party);

            var response = await client.PostAsync("/api/files/delete", IdBody(id));

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Empty(_factory.Storage.Deleted);
        }

        [Fact]
        public async Task Only_the_requested_file_is_touched()
        {
            var target = Guid.NewGuid();
            var other = Guid.NewGuid();
            _factory.Posts.Owns(target, Party, new StoredFile("PostFile", "a.png"));
            _factory.Posts.Owns(other, Party, new StoredFile("PostFile", "b.png"));
            using var client = _factory.CreateApiClient(Party);

            await client.PostAsync("/api/files/delete", IdBody(target));

            Assert.Equal("a.png", Assert.Single(_factory.Storage.Deleted).FileName);
            var stillThere = await _factory.Posts.DeleteOwnedAsync(other, Party);
            Assert.NotNull(stillThere);
        }
    }

    public class FilesControllerTests
    {
        [Fact]
        public void Controller_requires_authorization_and_is_routed_at_api_files()
        {
            var type = typeof(FilesController);

            Assert.NotNull(type.GetCustomAttribute<AuthorizeAttribute>());
            Assert.NotNull(type.GetCustomAttribute<ApiControllerAttribute>());
            Assert.Equal(
                new[] { "api/files" },
                type.GetCustomAttributes<RouteAttribute>().Select(r => r.Template).Order().ToArray());
        }

        [Fact]
        public void Delete_action_is_a_post_to_delete_with_the_body_bound()
        {
            var method = typeof(FilesController).GetMethod(nameof(FilesController.Delete))!;

            Assert.Equal("delete", method.GetCustomAttribute<HttpPostAttribute>()!.Template);
            Assert.NotNull(method.GetParameters()[0].GetCustomAttribute<FromBodyAttribute>());
            Assert.Null(method.GetCustomAttribute<AllowAnonymousAttribute>());
        }

        [Fact]
        public void Controller_exposes_exactly_one_action()
        {
            var actions = typeof(FilesController)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            Assert.Single(actions);
        }

        [Fact]
        public void Request_only_exposes_the_id_and_requires_it()
        {
            Assert.Equal(new[] { "Id" }, typeof(DeleteFileRequest).GetProperties().Select(p => p.Name));

            Assert.Contains(Validate(new DeleteFileRequest()), r => r.MemberNames.Contains("Id"));
            Assert.Empty(Validate(new DeleteFileRequest { Id = Guid.NewGuid() }));
        }

        private static List<ValidationResult> Validate(object model)
        {
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(model, new ValidationContext(model), results, true);
            return results;
        }
    }

    public class FilesWiringTests(RealWiringFactory factory) : IClassFixture<RealWiringFactory>
    {
        [Fact]
        public void Both_file_sources_are_registered_in_order()
        {
            using var scope = factory.Services.CreateScope();

            var names = scope.ServiceProvider.GetServices<IFileSource>().Select(s => s.Name).ToArray();

            Assert.Equal(new[] { "PostFile", "PartyFile" }, names);
        }

        [Fact]
        public void Sources_resolve_to_the_dapper_implementations()
        {
            using var scope = factory.Services.CreateScope();

            var types = scope.ServiceProvider.GetServices<IFileSource>().Select(s => s.GetType()).ToArray();

            Assert.Equal(
                new[]
                {
                    typeof(SalesManagement.Persistence.Features.Files.PostFileSource),
                    typeof(SalesManagement.Persistence.Features.Files.PartyFileSource),
                },
                types);
        }

        [Fact]
        public void Handler_and_real_file_storage_are_registered()
        {
            using var scope = factory.Services.CreateScope();

            Assert.NotNull(scope.ServiceProvider.GetRequiredService<DeleteFileHandler>());
            Assert.IsType<SalesManagement.Infrastructure.Storage.LocalFileStorage>(
                scope.ServiceProvider.GetRequiredService<IFileStorage>());
        }

        [Fact]
        public void Files_controller_can_be_activated()
        {
            using var scope = factory.Services.CreateScope();

            Assert.NotNull(ActivatorUtilities.CreateInstance<FilesController>(scope.ServiceProvider));
        }

        [Fact]
        public async Task Swagger_lists_only_the_plural_delete_route()
        {
            using var client = factory.CreateClient();

            var json = await client.GetStringAsync("/swagger/v1/swagger.json");

            Assert.Contains("\"/api/files/delete\"", json);
            Assert.DoesNotContain("\"/api/file/delete\"", json);
        }
    }
}
