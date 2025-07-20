using System;
using System.Data;
using System.Data.Common;

namespace UiPath.Database.BulkOps
{
    public interface IBulkOperations
    {
        DbConnection Connection { get; set; }
        string TableName { get; set; }
        Type BulkCopyType { get; set; }

        void WriteToServer(DataTable dataTable);
    }

    public class BulkOperationsFactory
    {
        public static IBulkOperations Create(DbConnection connection)
        {
            if (connection is Microsoft.Data.SqlClient.SqlConnection)
            {
                return new SQLBulkOperations();
            }
            if (connection is Oracle.ManagedDataAccess.Client.OracleConnection)
            {
                return new OracleBulkOperations();
            }
            return null;
        }
    }
}