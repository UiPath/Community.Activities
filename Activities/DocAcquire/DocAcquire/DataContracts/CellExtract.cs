namespace DocAcquire.DataContracts
{
    public class CellExtract
    {
        public string Text { get; set; }
        public bool IsValid { get; set; }
        public string UniqueId { get; set; }
        public string ValidationMessage { get; set; }
    }
}
