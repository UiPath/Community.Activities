using System;
using System.Data;
using System.Data.SqlClient;

namespace UiPath.Database.BulkOps
{
    public class SQLBulkOperations : IBulkOperations
    {
        public string Connection { get; set; }
        public string TableName { get; set; }
        public Type BulkCopyType { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public void WriteToServer(DataTable dataTable)
        {
            // Set up the bulk copy object
            using (SqlBulkCopy bulkCopy = new SqlBulkCopy(Connection, SqlBulkCopyOptions.UseInternalTransaction))
            {
                bulkCopy.DestinationTableName = TableName;
                bulkCopy.WriteToServer(dataTable);
            }
        }
    }
}