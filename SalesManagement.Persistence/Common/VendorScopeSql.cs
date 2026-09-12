namespace SalesManagement.Persistence.Common
{
    internal static class VendorScopeSql
    {
        public const string Declare = """
            DECLARE @VendorCode NVARCHAR(50) = (
                SELECT dl.Code
                FROM GNR.Party AS me
                    INNER JOIN ACC.DL AS dl ON dl.DLId = me.DLRef
                WHERE me.PartyId = @PartyId);
            """;
    }
}
