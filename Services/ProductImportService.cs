using ECommerceCsvImport.Models;
using System.Globalization;

namespace ECommerceCsvImport.Services
{
    public class ProductImportService : IProductImportService
    {
        private readonly IProductRepository _repository;

        private static readonly string[] RequiredHeaders = new[]
        {
            "sku", "name", "description", "price", "currency", "category_name",
            "stock", "image_url", "is_active", "brand", "warranty_months", "model_number"
        };

        public ProductImportService(IProductRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public ImportResult ImportFromCsv(string filePath)
        {
            var result = new ImportResult();

            if (string.IsNullOrWhiteSpace(filePath))
            {
                result.Errors.Add(new ImportError { RowNumber = 0, Message = "Файл не найден" });
                return result;
            }

            if (!File.Exists(filePath))
            {
                result.Errors.Add(new ImportError { RowNumber = 0, Message = "Файл не найден" });
                return result;
            }

            try
            {
                var lines = File.ReadAllLines(filePath);
                return ImportFromLines(lines);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                result.Errors.Add(new ImportError { RowNumber = 0, Message = $"Ошибка чтения файла: {ex.Message}" });
                return result;
            }
        }

        public ImportResult ImportFromLines(IEnumerable<string> lines)
        {
            var result = new ImportResult();
            var lineList = lines.ToList();

            if (lineList.Count == 0 || lineList.All(string.IsNullOrWhiteSpace))
            {
                result.Errors.Add(new ImportError { RowNumber = 0, Message = "Данные для импорта отсутствуют" });
                return result;
            }

            int headerIndex = lineList.FindIndex(l => !string.IsNullOrWhiteSpace(l));
            string headerLine = lineList[headerIndex];
            int headerRowNumber = headerIndex + 1;

            var headers = CsvLineParser.Parse(headerLine);

            var columnMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < headers.Count; i++)
            {
                if (!columnMap.ContainsKey(headers[i]))
                {
                    columnMap[headers[i]] = i;
                }
            }

            bool hasAllHeaders = RequiredHeaders.All(h => columnMap.ContainsKey(h));
            if (!hasAllHeaders)
            {
                result.Errors.Add(new ImportError { RowNumber = headerRowNumber, Message = "Неверные заголовки CSV" });
                return result;
            }

            var importedSkus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = headerIndex + 1; i < lineList.Count; i++)
            {
                string line = lineList[i];
                if (string.IsNullOrWhiteSpace(line)) continue;

                result.TotalRows++;
                int rowNumber = i + 1;

                var values = CsvLineParser.Parse(line);

                if (values.Count != headers.Count)
                {
                    result.Errors.Add(new ImportError { RowNumber = rowNumber, Message = "Неверное количество полей в строке" });
                    continue;
                }

                string GetValue(string columnName)
                {
                    if (columnMap.TryGetValue(columnName, out int index) && index < values.Count)
                        return values[index];
                    return string.Empty;
                }

                var rowErrors = new List<ImportError>();

                string sku = GetValue("sku").Trim();
                if (string.IsNullOrWhiteSpace(sku))
                    rowErrors.Add(new ImportError { RowNumber = rowNumber, Message = "Поле sku не может быть пустым" });

                string name = GetValue("name").Trim();
                if (string.IsNullOrWhiteSpace(name))
                    rowErrors.Add(new ImportError { RowNumber = rowNumber, Message = "Поле name не может быть пустым" });

                string priceStr = GetValue("price").Trim();
                bool isPriceValid = decimal.TryParse(priceStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal price) && price > 0;
                if (!isPriceValid)
                    rowErrors.Add(new ImportError { RowNumber = rowNumber, Message = "Поле price должно быть числом больше 0" });

                string stockStr = GetValue("stock").Trim();
                bool isStockValid = int.TryParse(stockStr, out int stock) && stock >= 0;
                if (!isStockValid)
                    rowErrors.Add(new ImportError { RowNumber = rowNumber, Message = "Остаток на складе должен быть целым числом не меньше 0" });

                string activeStr = GetValue("is_active").Trim().ToLowerInvariant();
                bool isActiveValid = activeStr == "true" || activeStr == "false" || activeStr == "1" || activeStr == "0";
                bool isActive = isActiveValid && (activeStr == "true" || activeStr == "1");
                if (!isActiveValid)
                    rowErrors.Add(new ImportError { RowNumber = rowNumber, Message = "Поле is_active должно иметь значение true, false, 1 или 0" });

                string brand = GetValue("brand").Trim();
                if (string.IsNullOrWhiteSpace(brand))
                    rowErrors.Add(new ImportError { RowNumber = rowNumber, Message = "Поле brand обязательно для варианта «Электроника»" });

                string warrantyStr = GetValue("warranty_months").Trim();
                bool isWarrantyValid = int.TryParse(warrantyStr, out int warrantyMonths) && warrantyMonths >= 0 && warrantyMonths <= 60;
                if (!isWarrantyValid)
                    rowErrors.Add(new ImportError { RowNumber = rowNumber, Message = "Гарантия должна быть целым числом от 0 до 60 месяцев" });

                if (rowErrors.Count > 0)
                {
                    result.Errors.AddRange(rowErrors);
                    continue;
                }

                bool isDuplicate = _repository.GetBySku(sku) != null || importedSkus.Contains(sku);
                if (isDuplicate)
                {
                    result.Errors.Add(new ImportError { RowNumber = rowNumber, Message = "Артикул товара уже существует" });
                    continue;
                }

                var product = new Product
                {
                    Sku = sku,
                    Name = name,
                    Description = GetValue("description").Trim(),
                    Price = price,
                    Currency = NormalizeCurrency(GetValue("currency")),
                    CategoryName = NormalizeCategory(GetValue("category_name")),
                    StockQuantity = stock,
                    ImageUrl = GetValue("image_url").Trim(),
                    IsActive = isActive,
                    Brand = brand,
                    WarrantyMonths = warrantyMonths,
                    ModelNumber = GetValue("model_number").Trim()
                };

                _repository.Add(product);
                importedSkus.Add(sku);
                result.ImportedCount++;
            }

            if (result.TotalRows == 0 && result.Errors.Count == 0)
            {
                result.Errors.Add(new ImportError { RowNumber = 0, Message = "Данные для импорта отсутствуют" });
            }

            return result;
        }

        private static string NormalizeCurrency(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "RUB";
            return value.Trim().ToUpperInvariant();
        }

        private static string NormalizeCategory(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "Без категории";
            return value.Trim();
        }
    }
}