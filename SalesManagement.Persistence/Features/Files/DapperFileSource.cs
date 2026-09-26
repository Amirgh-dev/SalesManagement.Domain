using System.Data;
using Dapper;
using SalesManagement.Domain.Features.Files;

namespace SalesManagement.Persistence.Features.Files
{
    public abstract class DapperFileSource(Func<IDbConnection> createConnection) : IFileSource
    {
        public abstract string Name { get; }

        protected abstract string DeleteSql { get; }

        public async Task<StoredFile?> DeleteOwnedAsync(
            Guid fileId, int partyId, CancellationToken cancellationToken = default)
        {
            using var connection = createConnection();
            var row = await connection.QuerySingleOrDefaultAsync<DeletedRow>(
                new CommandDefinition(
                    DeleteSql, new { Id = fileId, PartyId = partyId }, cancellationToken: cancellationToken));

            return row is null ? null : new StoredFile(row.Folder ?? "", row.FileName ?? "");
        }

        private sealed class DeletedRow
        {
            public string? Folder { get; set; }
            public string? FileName { get; set; }
        }
    }
}
