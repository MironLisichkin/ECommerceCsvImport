namespace ECommerceCsvImport.Models
{
    public class ImportError
    {
        public int RowNumber { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}