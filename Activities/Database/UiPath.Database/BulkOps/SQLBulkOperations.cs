using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UiPath.Database.BulkOps
{
    public class SQLBulkOperations : IBulkOperations
    {
        public string Connection { get; set; }
        public string TableName { get; set; }


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
