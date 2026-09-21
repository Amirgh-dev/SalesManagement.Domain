using System.IO.Compression;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SalesManagement.Infrastructure.Security;

namespace SalesManagement.API.Common
{
    public static class ApiServiceCollectionExtensions
    {
        public const string AuthRateLimiterPolicy = "auth";

        public static IServiceCollection AddApiServices(
            this IServiceCollection services, IConfiguration configuration)
        {
            services.AddJwtAuthentication(configuration);
            services.AddAuthorization();

            services.AddCorsPolicy(configuration);

            services.AddControllers();
            services.AddEndpointsApiExplorer();
            services.AddSwaggerWithBearer();
            services.AddProblemDetails();

            services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
                options.Providers.Add<BrotliCompressionProvider>();
                options.Providers.Add<GzipCompressionProvider>();
            });
            services.Configure<BrotliCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);
            services.Configure<GzipCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);

            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.AddPolicy(AuthRateLimiterPolicy, context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 20,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0
                        }));
            });

            return services;
        }

        public const string CorsPolicyName = "frontend";

        private static IServiceCollection AddCorsPolicy(
            this IServiceCollection services, IConfiguration configuration)
        {
            var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?.Where(o => !string.IsNullOrWhiteSpace(o))
                .Select(o => o.Trim().TrimEnd('/'))
                .ToArray() ?? [];

            services.AddCors(options => options.AddPolicy(CorsPolicyName, policy =>
            {
                if (origins.Length == 0)
                    policy.AllowAnyOrigin();
                else
                    policy.WithOrigins(origins);

                policy.AllowAnyHeader()
                      .AllowAnyMethod()
                      .WithExposedHeaders("Retry-After");
            }));

            return services;
        }

        private static IServiceCollection AddJwtAuthentication(
            this IServiceCollection services, IConfiguration configuration)
        {
            var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
                              ?? throw new InvalidOperationException("بخش JwtSettings پیدا نشد");

            if (string.IsNullOrWhiteSpace(jwtSettings.SecretKey))
                throw new InvalidOperationException(
                    "JwtSettings:SecretKey تنظیم نشده است (از User Secrets یا متغیر محیطی مقداردهی شود)");

            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtSettings.Issuer,
                        ValidAudience = jwtSettings.Audience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                        ClockSkew = TimeSpan.FromMinutes(1)
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnTokenValidated = async context =>
                        {
                            if (!Guid.TryParse(context.Principal?.FindFirst("jti")?.Value, out var jti)) return;

                            var store = context.HttpContext.RequestServices
                                .GetRequiredService<SalesManagement.Domain.Features.Auth.PartyLogin.IRevokedTokenStore>();

                            if (await store.IsRevokedAsync(jti, context.HttpContext.RequestAborted))
                                context.Fail("توکن باطل شده است");
                        }
                    };
                });

            return services;
        }

        private static IServiceCollection AddSwaggerWithBearer(this IServiceCollection services)
        {
            services.AddSwaggerGen(options =>
            {
                var scheme = new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = JwtBearerDefaults.AuthenticationScheme
                    }
                };

                options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, scheme);
                options.AddSecurityRequirement(new OpenApiSecurityRequirement { [scheme] = Array.Empty<string>() });
            });

            return services;
        }
    }
}
