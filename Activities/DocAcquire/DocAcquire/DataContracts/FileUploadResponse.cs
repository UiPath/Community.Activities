namespace DocAcquire.DataContracts
{
    public class FileUploadResponse 
    {
        public int DocumentId { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
    }
}