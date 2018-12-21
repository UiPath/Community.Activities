using System.Collections.Generic;

namespace DocAcquire.DataContracts
{
    public class DocumentExtractResponse
    {
        public DocumentExtractResponse()
        {
            this.Fields = new List<FieldExtract>();
            this.Tables = new List<TableExtract>();
        }

        public List<FieldExtract> Fields { get; set; }

        public List<TableExtract> Tables { get; set; }

        public string ValidationSummary { get; set; }

        public ResultStatus Result { get; set; }

        public bool IsValid { get; set; }

        private bool AreValidFields { get; set; }

        private bool AreValidTables { get; set; }

        public int Confidence { get; set; }

    }
}
