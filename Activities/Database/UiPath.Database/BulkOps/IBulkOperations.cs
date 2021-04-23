using System;
using System.Data;

namespace UiPath.Database.BulkOps
{
    public interface IBulkOperations
    {
        string Connection { get; set; }
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