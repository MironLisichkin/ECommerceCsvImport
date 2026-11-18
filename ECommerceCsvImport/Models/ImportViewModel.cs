namespace ECommerceCsvImport.Models
{
    public class ImportViewModel
    {
        public string FilePath { get; set; } = string.Empty;
        public ImportResult? Result { get; set; }
    }
}