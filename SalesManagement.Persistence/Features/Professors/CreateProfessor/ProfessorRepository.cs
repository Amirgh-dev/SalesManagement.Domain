using System.Data.Common;
using Dapper;
using SalesManagement.Domain.Common.Exceptions;
using SalesManagement.Domain.Features.Professors;
using SalesManagement.Persistence.ConnectionFactories;

namespace SalesManagement.Persistence.Features.Professors.CreateProfessor
{
    public class ProfessorRepository(IZanisServicesConnectionFactory connectionFactory) : IProfessorRepository
    {
        private const string SqlInsert = """
            INSERT INTO dbo.Professors (Id, Name, LastName, Specialization, IsOk)
            VALUES (@Id, @Name, @LastName, @Specialization, @IsOk);
            """;

        private const string SqlUpdate = """
            UPDATE dbo.Professors
            SET Name = @Name, LastName = @LastName, Specialization = @Specialization, IsOk = @IsOk
            WHERE Id = @Id;
            """;

        public async Task<Guid> SaveAsync(NewProfessor professor, CancellationToken cancellationToken = default)
        {
            await using var connection = (DbConnection)connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            var id = professor.Id ?? Guid.NewGuid();
            var args = new { Id = id, professor.Name, professor.LastName, professor.Specialization, professor.IsOk };

            if (professor.IsUpdate)
            {
                var affected = await connection.ExecuteAsync(
                    new CommandDefinition(SqlUpdate, args, cancellationToken: cancellationToken));
                if (affected == 0)
                    throw new DomainException("استاد پیدا نشد");
            }
            else
            {
                await connection.ExecuteAsync(
                    new CommandDefinition(SqlInsert, args, cancellationToken: cancellationToken));
            }

            return id;
        }
    }
}
