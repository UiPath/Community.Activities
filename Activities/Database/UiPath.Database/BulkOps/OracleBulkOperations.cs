using System;
using System.Data;

namespace UiPath.Database.BulkOps
{
    public class OracleBulkOperations : IBulkOperations
    {
        public string Connection { get; set; }
        public string TableName { get; set; }
        public Type BulkCopyType { get; set; }

        public void WriteToServer(DataTable dataTable)
        {
            dynamic bulkCopy = Activator.CreateInstance(BulkCopyType, new object[] { Connection });

            bulkCopy.DestinationTableName = TableName;
            bulkCopy.WriteToServer(dataTable);
        }
    }
}