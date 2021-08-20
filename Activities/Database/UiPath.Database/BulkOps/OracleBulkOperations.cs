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

        public void WriteToServer(DataTable dataTable)
        {

            //dynamic bulkCopy = Activator.CreateInstance(BulkCopyType, new object[] { Connection });
            OracleBulkCopy bulkCopy = new OracleBulkCopy((OracleConnection)Connection);
            bulkCopy.DestinationTableName = TableName;
            bulkCopy.WriteToServer(dataTable);
        }
    }
}