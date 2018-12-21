using System.Collections.Generic;

namespace DocAcquire.DataContracts
{
    public class RowExtract
    {
        public RowExtract()
        {
            this.Cells = new List<CellExtract>();
        }

        public List<CellExtract> Cells { get; set; }

        public int RowNo { get; set; }
        public bool IsHeader { get; set; }
        public bool IsValid { get; set; }
        public string ValidationMessage { get; set; }
    }
}
