using System.Data.Common;
using Dapper;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SalesManagement.Persistence.ConnectionFactories;

namespace SalesManagement.Persistence.Features.Auth.Logout
{
    public sealed class RevokedTokenPurgeService(
        IZanisServicesConnectionFactory connectionFactory,
        TimeProvider timeProvider,
        ILogger<RevokedTokenPurgeService> logger) : BackgroundService
    {
        private static readonly TimeSpan PurgeInterval = TimeSpan.FromHours(24);

        private const string SqlPurge = "DELETE FROM dbo.RevokedTokens WHERE ExpiresAt <= @Now;";

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(PurgeInterval);

            do
            {
                try
                {
                    await using var connection = (DbConnection)connectionFactory.CreateConnection();
                    await connection.OpenAsync(stoppingToken);

                    var deleted = await connection.ExecuteAsync(new CommandDefinition(
                        SqlPurge, new { Now = timeProvider.GetUtcNow().UtcDateTime },
                        cancellationToken: stoppingToken));

                    if (deleted > 0)
                        logger.LogInformation(
                            "پاکسازی دوره‌ای RevokedTokens: {Count} رکورد منقضی حذف شد", deleted);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogWarning(ex, "پاکسازی دوره‌ای جدول RevokedTokens ناموفق بود");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
    }
}
