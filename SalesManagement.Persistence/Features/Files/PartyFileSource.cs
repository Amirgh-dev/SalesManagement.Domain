using SalesManagement.Persistence.ConnectionFactories;

namespace SalesManagement.Persistence.Features.Files
{
    public sealed class PartyFileSource(IZanisServicesConnectionFactory connectionFactory)
        : DapperFileSource(connectionFactory.CreateConnection)
    {
        private const string Sql = """
            DELETE FROM dbo.PartyFile
            OUTPUT deleted.FileName AS FileName, deleted.[Tables] AS Folder
            WHERE Id = @Id AND PartyRef = @PartyId AND [Tables] = N'Profile';
            """;

        public override string Name => "PartyFile";

        protected override string DeleteSql => Sql;
    }
}
