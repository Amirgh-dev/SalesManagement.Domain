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

            services.AddControllers()
                .ConfigureApiBehaviorOptions(o => o.InvalidModelStateResponseFactory = ValidationProblemFactory.Create);
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
                      .WithExposedHeaders("Retry-After", "X-New-Token");
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
                            {
                                context.Fail("توکن باطل شده است");
                                return;
                            }

                            var iatValue = context.Principal?.FindFirst(
                                System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Iat)?.Value;
                            if (!long.TryParse(iatValue, out var iatUnixSeconds))
                                iatUnixSeconds = 0;

                            var issuedAt = DateTimeOffset.FromUnixTimeSeconds(iatUnixSeconds).UtcDateTime;
                            var elapsedSinceIssued = DateTime.UtcNow - issuedAt;
                            if (elapsedSinceIssued < TimeSpan.FromMinutes(jwtSettings.RefreshIntervalMinutes))
                                return;

                            var sub = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                                      ?? context.Principal?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
                            var phone = context.Principal?.FindFirst("phone")?.Value;
                            if (!int.TryParse(sub, out var partyId) || string.IsNullOrEmpty(phone))
                                return;

                            var tokenGenerator = context.HttpContext.RequestServices
                                .GetRequiredService<SalesManagement.Application.Common.Interfaces.ITokenGenerator>();

                            context.HttpContext.Response.Headers["X-New-Token"] = tokenGenerator.GenerateAccessToken(partyId, phone);
                        },
                        OnChallenge = async context =>
                        {
                            context.HandleResponse();

                            var detail = context.AuthenticateFailure switch
                            {
                                Microsoft.IdentityModel.Tokens.SecurityTokenExpiredException =>
                                    "توکن منقضی شده است، دوباره وارد شوید",
                                { } => "توکن نامعتبر است",
                                null => "برای این عملیات ابتدا باید وارد شوید",
                            };

                            await ProblemResponseWriter.WriteAsync(
                                context.HttpContext, System.Net.HttpStatusCode.Unauthorized, "خطای احراز هویت", detail);
                        },
                        OnForbidden = context =>
                            ProblemResponseWriter.WriteAsync(
                                context.HttpContext, System.Net.HttpStatusCode.Forbidden,
                                "دسترسی غیرمجاز", "شما اجازه‌ی انجام این عملیات را ندارید"),
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
