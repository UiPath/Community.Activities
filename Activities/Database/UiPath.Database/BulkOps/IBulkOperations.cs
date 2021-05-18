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
        public static IBulkOperations Create(string providerName)
        {
            if (providerName == "System.Data.SqlClient")
            {
                return new SQLBulkOperations();
            }
            if (providerName == "Oracle.ManagedDataAccess.Client")
            {
                return new OracleBulkOperations();
            }
            return null;
        }
    }
}