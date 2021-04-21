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
}