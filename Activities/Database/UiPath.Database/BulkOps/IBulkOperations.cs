using System;
using System.Data;
using System.Data.Common;
using UiPath.Database.Extensibility;

namespace UiPath.Database.BulkOps
{
    public interface IBulkOperations
    {
        DbConnection Connection { get; set; }
        string TableName { get; set; }
        Type BulkCopyType { get; set; }

        void WriteToServer(DataTable dataTable, TimeSpan? commandTimeout = null);
    }

    public class BulkOperationsFactory
    {
        public static IBulkOperations Create(DbConnection connection)
        {
            if (connection is Microsoft.Data.SqlClient.SqlConnection)
            {
                return new SQLBulkOperations();
            }
            // Other providers (Oracle) come from the extended package, when it is installed.
            return DatabaseProviders.FindFor(connection)?.CreateBulkOperations();
        }
    }
}