using System.Data;
using System.Data.Common;
using Dapper;
using SalesManagement.Domain.Common.Exceptions;

namespace SalesManagement.Persistence.Common
{
    internal static class SepidarDb
    {
        public static async Task<int> NextIdAsync(
            DbConnection connection, DbTransaction transaction, string tableName)
        {
            await AcquireIdLockAsync(connection, transaction, tableName);

            var p = new DynamicParameters();
            p.Add("@TableName", tableName);
            p.Add("@IncValue", 1);
            p.Add("@Id", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await connection.ExecuteAsync(
                "FMK.spGetNextId", p, transaction, commandType: CommandType.StoredProcedure);
            return p.Get<int>("@Id");
        }

        private static async Task AcquireIdLockAsync(
            DbConnection connection, DbTransaction transaction, string tableName)
        {
            var p = new DynamicParameters();
            p.Add("@Resource", $"NextId:{tableName}");
            p.Add("@LockMode", "Exclusive");
            p.Add("@LockOwner", "Transaction");
            p.Add("@LockTimeout", 30000);
            p.Add("@Result", dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);

            await connection.ExecuteAsync(
                "sp_getapplock", p, transaction, commandType: CommandType.StoredProcedure);

            if (p.Get<int>("@Result") < 0)
                throw new IdGenerationCollisionException(
                    "امکان دریافت شناسه در حال حاضر وجود ندارد، لطفاً دوباره تلاش کنید");
        }

        public static async Task AcquireNamedLockAsync(
            DbConnection connection, DbTransaction transaction, string resource)
        {
            var p = new DynamicParameters();
            p.Add("@Resource", resource);
            p.Add("@LockMode", "Exclusive");
            p.Add("@LockOwner", "Transaction");
            p.Add("@LockTimeout", 10000);
            p.Add("@Result", dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);

            await connection.ExecuteAsync(
                "sp_getapplock", p, transaction, commandType: CommandType.StoredProcedure);

            if (p.Get<int>("@Result") < 0)
                throw new DomainException("سیستم در حال پردازش درخواست مشابه است، لطفاً چند لحظه بعد دوباره تلاش کنید");
        }
    }
}
