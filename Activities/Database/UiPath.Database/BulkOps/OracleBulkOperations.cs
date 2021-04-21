using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UiPath.Database.BulkOps
{
    public class OracleBulkOperations : IBulkOperations
    {
        public string Connection { get; set; }
        public string TableName { get; set; }
        public void WriteToServer(DataTable dataTable)
        {
            throw new NotImplementedException();
        }
    }
}
