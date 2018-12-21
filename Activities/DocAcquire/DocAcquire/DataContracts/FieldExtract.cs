namespace DocAcquire.DataContracts
{
    public class FieldExtract
    {
        public FieldExtract()
        {
            this.Value = string.Empty;
        }

        public string Field { get; set; }
        public int Confidence { get; set; }
        public string Value { get; set; }
        public bool IsValid { get; set; }
        public string ValidationMessage { get; set; }
        public string UniqueId { get; set; }
    }
}
