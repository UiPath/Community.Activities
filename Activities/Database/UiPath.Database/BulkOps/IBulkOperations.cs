using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UiPath.Database.BulkOps
{

    public interface IBulkOperations
    {
        string Connection { get; set; }
        string TableName { get; set; }
        void WriteToServer(DataTable dataTable);
    }
}
