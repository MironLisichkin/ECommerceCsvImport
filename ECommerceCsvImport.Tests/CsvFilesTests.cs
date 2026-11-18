using Xunit;
using ECommerceCsvImport.Services;

namespace ECommerceCsvImport.Tests
{
    /// <summary>
    /// Тесты импорта из реальных CSV-файлов папки testdata (раздел 2.1 плана).
    /// Файлы копируются в выходную папку тестового проекта (см. .csproj).
    /// </summary>
    public class CsvFilesTests
    {
        private static string DataPath(string fileName) =>
            Path.Combine(AppContext.BaseDirectory, "testdata", fileName);

        private static (ProductImportService Service, InMemoryProductRepository Repo) Create()
        {
            var repo = new InMemoryProductRepository();
            return (new ProductImportService(repo), repo);
        }

        [Fact]
        public void ValidElectronicsFile_ImportsTwoProducts()
        {
            var (service, repo) = Create();

            var result = service.ImportFromCsv(DataPath("valid_electronics.csv"));

            Assert.Equal(2, result.TotalRows);
            Assert.Equal(2, result.ImportedCount);
            Assert.Equal(0, result.ErrorCount);
            Assert.Equal(2, repo.Count);
        }

        [Fact]
        public void InvalidPriceFile_ReportsErrorOnRow2()
        {
            var (service, repo) = Create();

            var result = service.ImportFromCsv(DataPath("invalid_price.csv"));

            Assert.Equal(1, result.TotalRows);
            Assert.Equal(0, result.ImportedCount);
            Assert.Equal(1, result.ErrorCount);
            Assert.Equal(2, result.Errors[0].RowNumber);
            Assert.Contains("Поле price должно быть числом больше 0", result.Errors[0].Message);
            Assert.Equal(0, repo.Count);
        }

        [Fact]
        public void DuplicateSkuFile_KeepsOnlyFirstProduct()
        {
            var (service, repo) = Create();

            var result = service.ImportFromCsv(DataPath("duplicate_sku.csv"));

            Assert.Equal(2, result.TotalRows);
            Assert.Equal(1, result.ImportedCount);
            Assert.Equal(1, result.ErrorCount);
            Assert.Contains("Артикул товара уже существует", result.Errors[0].Message);
            Assert.Equal(1, repo.Count);
            Assert.Equal("Ноутбук Lenovo IdeaPad 3", repo.GetBySku("1001")!.Name);
        }

        [Fact]
        public void MissingBrandFile_ReportsBrandError()
        {
            var (service, _) = Create();

            var result = service.ImportFromCsv(DataPath("missing_brand.csv"));

            Assert.Equal(1, result.TotalRows);
            Assert.Equal(0, result.ImportedCount);
            Assert.Equal(1, result.ErrorCount);
            Assert.Contains("Поле brand обязательно для варианта «Электроника»", result.Errors[0].Message);
        }

        [Fact]
        public void WarrantyOutOfRangeFile_ReportsWarrantyError()
        {
            var (service, _) = Create();

            var result = service.ImportFromCsv(DataPath("warranty_out_of_range.csv"));

            Assert.Equal(1, result.TotalRows);
            Assert.Equal(0, result.ImportedCount);
            Assert.Equal(1, result.ErrorCount);
            Assert.Contains("Гарантия должна быть целым числом от 0 до 60 месяцев", result.Errors[0].Message);
        }

        [Fact]
        public void BadActiveFile_ReportsIsActiveError()
        {
            var (service, _) = Create();

            var result = service.ImportFromCsv(DataPath("bad_active.csv"));

            Assert.Equal(1, result.TotalRows);
            Assert.Equal(0, result.ImportedCount);
            Assert.Equal(1, result.ErrorCount);
            Assert.Contains("Поле is_active должно иметь значение true, false, 1 или 0", result.Errors[0].Message);
        }

        [Fact]
        public void EmptyFile_ReportsNoDataError()
        {
            var (service, _) = Create();

            var result = service.ImportFromCsv(DataPath("empty_file.csv"));

            Assert.Equal(0, result.TotalRows);
            Assert.Equal(0, result.ImportedCount);
            Assert.Equal(1, result.ErrorCount);
            Assert.Contains("Данные для импорта отсутствуют", result.Errors[0].Message);
        }
    }
}
