using Oracle.ManagedDataAccess.Client;
using System;
using System.Data;
using System.Data.Common;

namespace UiPath.Database.BulkOps
{
    public class OracleBulkOperations : IBulkOperations
    {
        public DbConnection Connection { get; set; }
        public string TableName { get; set; }
        public Type BulkCopyType { get; set; }

        public void WriteToServer(DataTable dataTable, int? commandTimeoutMs = null)
        {
            OracleBulkCopy bulkCopy = new OracleBulkCopy((OracleConnection)Connection);
            if (commandTimeoutMs.HasValue)
            {
                var seconds = (int)Math.Ceiling((double)commandTimeoutMs.Value / 1000);
                if (seconds != 0)
                    bulkCopy.BulkCopyTimeout = seconds;
            }
            bulkCopy.DestinationTableName = TableName;
            bulkCopy.WriteToServer(dataTable);
        }
    }
}