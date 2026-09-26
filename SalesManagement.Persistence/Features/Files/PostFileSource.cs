using SalesManagement.Persistence.ConnectionFactories;

namespace SalesManagement.Persistence.Features.Files
{
    public sealed class PostFileSource(IZanisServicesConnectionFactory connectionFactory)
        : DapperFileSource(connectionFactory.CreateConnection)
    {
        private const string Sql = """
            DELETE pf
            OUTPUT deleted.FileName AS FileName, deleted.[Tables] AS Folder
            FROM dbo.PostFile AS pf
                INNER JOIN dbo.ProfileParty AS p ON p.Id = pf.ProfileRef
            WHERE pf.Id = @Id AND p.PartyRef = @PartyId;
            """;

        public override string Name => "PostFile";

        protected override string DeleteSql => Sql;
    }
}
