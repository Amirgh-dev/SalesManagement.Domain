using System.Collections.Concurrent;
using System.Data.Common;
using Dapper;
using Microsoft.Extensions.Logging;
using SalesManagement.Domain.Features.Auth.PartyLogin;
using SalesManagement.Persistence.ConnectionFactories;

namespace SalesManagement.Persistence.Features.Auth.Logout
{
    public sealed class RevokedTokenStore(
        IZanisServicesConnectionFactory connectionFactory,
        TimeProvider timeProvider,
        ILogger<RevokedTokenStore> logger) : IRevokedTokenStore
    {
        private static readonly TimeSpan RetryLoadAfter = TimeSpan.FromMinutes(1);

        private const string SqlLoad = "SELECT Jti, ExpiresAt FROM dbo.RevokedTokens WHERE ExpiresAt > @Now;";
        private const string SqlPurge = "DELETE FROM dbo.RevokedTokens WHERE ExpiresAt <= @Now;";

        private const string SqlInsert = """
            IF NOT EXISTS (SELECT 1 FROM dbo.RevokedTokens WHERE Jti = @Jti)
                INSERT INTO dbo.RevokedTokens (Jti, ExpiresAt) VALUES (@Jti, @ExpiresAt);
            """;

        private readonly ConcurrentDictionary<Guid, DateTimeOffset> _revoked = new();
        private readonly SemaphoreSlim _loadLock = new(1, 1);
        private volatile bool _loaded;
        private DateTimeOffset _lastLoadAttempt = DateTimeOffset.MinValue;

        public async Task RevokeAsync(Guid jti, DateTimeOffset expiresAt, CancellationToken cancellationToken = default)
        {
            await using var connection = (DbConnection)connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);
            await connection.ExecuteAsync(new CommandDefinition(
                SqlInsert, new { Jti = jti, ExpiresAt = expiresAt.UtcDateTime }, cancellationToken: cancellationToken));

            _revoked[jti] = expiresAt;
        }

        public async Task<bool> IsRevokedAsync(Guid jti, CancellationToken cancellationToken = default)
        {
            await EnsureLoadedAsync(cancellationToken);
            return _revoked.TryGetValue(jti, out var expiresAt) && expiresAt > timeProvider.GetUtcNow();
        }

        private async Task EnsureLoadedAsync(CancellationToken cancellationToken)
        {
            if (_loaded) return;

            await _loadLock.WaitAsync(cancellationToken);
            try
            {
                var now = timeProvider.GetUtcNow();
                if (_loaded || now - _lastLoadAttempt < RetryLoadAfter) return;
                _lastLoadAttempt = now;

                await using var connection = (DbConnection)connectionFactory.CreateConnection();
                await connection.OpenAsync(cancellationToken);

                var rows = await connection.QueryAsync<(Guid Jti, DateTime ExpiresAt)>(
                    new CommandDefinition(SqlLoad, new { Now = now.UtcDateTime }, cancellationToken: cancellationToken));
                foreach (var (jti, expiresAt) in rows)
                    _revoked[jti] = new DateTimeOffset(DateTime.SpecifyKind(expiresAt, DateTimeKind.Utc));

                await connection.ExecuteAsync(new CommandDefinition(
                    SqlPurge, new { Now = now.UtcDateTime }, cancellationToken: cancellationToken));

                _loaded = true;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "بارگذاری لیست توکن‌های باطل‌شده ناموفق بود؛ بعداً دوباره تلاش می‌شود");
            }
            finally
            {
                _loadLock.Release();
            }
        }
    }
}
