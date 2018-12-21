using System.Collections.Generic;

namespace DocAcquire.DataContracts
{
    public class TableExtract
    {
        public TableExtract()
        {
            this.Rows = new List<RowExtract>();
        }
        public List<RowExtract> Rows { get; set; }

        public string Name { get; set; }

        public bool IsValid { get; set; }
        public string ValidationMessage { get; set; }
    }
}
