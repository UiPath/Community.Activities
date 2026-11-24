using System;
using System.Data;
using System.Data.Common;
using UiPath.Database.Extensibility;

namespace UiPath.Database.BulkOps
{
    /// <remarks>
    /// This interface is implemented by extension packages that can be installed at a different version than the base
    /// package (for example the Oracle bulk copy of the extended package). Never add, remove or change a member: a
    /// mismatch fails with a <see cref="TypeLoadException"/> when a bulk operation runs. Add new capabilities as
    /// separate, optional interfaces.
    /// </remarks>
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