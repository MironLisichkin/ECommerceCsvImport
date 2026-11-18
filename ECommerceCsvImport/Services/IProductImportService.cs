using ECommerceCsvImport.Models;

namespace ECommerceCsvImport.Services
{
    public interface IProductImportService
    {
        ImportResult ImportFromCsv(string filePath);
        ImportResult ImportFromLines(IEnumerable<string> lines);
    }
}