using Xunit;
using ECommerceCsvImport.Models;
using ECommerceCsvImport.Services;
using System.Collections.Generic;
using System.IO;

namespace ECommerceCsvImport.Tests
{
    public class ProductImportServiceTests
    {
        private const string Header = "sku,name,description,price,currency,category_name,stock,image_url,is_active,brand,warranty_months,model_number";

        private ProductImportService GetService(out InMemoryProductRepository repo)
        {
            repo = new InMemoryProductRepository();
            return new ProductImportService(repo);
        }

        // Тест 1: Успешный импорт одной корректной строки
        [Fact]
        public void ImportFromLines_ValidLine_AddsProduct()
        {
            var service = GetService(out var repo);
            var lines = new[] { Header, "1001,Ноутбук Lenovo IdeaPad 3,Бюджетный ноутбук для работы и учебы,45990,RUB,Компьютеры,5,/images/laptop.jpg,true,Lenovo,24,IdeaPad 3-15" };

            var result = service.ImportFromLines(lines);

            Assert.Equal(1, result.TotalRows);
            Assert.Equal(1, result.ImportedCount);
            Assert.Empty(result.Errors);

            var product = repo.GetBySku("1001");
            Assert.NotNull(product);
            Assert.Equal("Ноутбук Lenovo IdeaPad 3", product.Name);
            Assert.Equal(45990m, product.Price);
            Assert.Equal("Lenovo", product.Brand);
            Assert.Equal(24, product.WarrantyMonths);
            Assert.Equal("IdeaPad 3-15", product.ModelNumber);
        }

        // Тест 2: Успешный импорт нескольких строк
        [Fact]
        public void ImportFromLines_MultipleValidLines_AddsAll()
        {
            var service = GetService(out var repo);
            var lines = new[] {
                Header,
                "1001,Товар1,Описание,100,RUB,Кат,5,/img.jpg,true,Brand,12,Model",
                "1002,Товар2,Описание,200,RUB,Кат,5,/img.jpg,true,Brand,12,Model"
            };

            var result = service.ImportFromLines(lines);

            Assert.Equal(2, result.TotalRows);
            Assert.Equal(2, result.ImportedCount);
            Assert.Empty(result.Errors);
            Assert.Equal(2, repo.Count);
        }

        // Тест 3: Файл не найден
        [Fact]
        public void ImportFromCsv_FileNotFound_ReturnsError()
        {
            var service = GetService(out _);
            var path = Path.Combine(Path.GetTempPath(), System.Guid.NewGuid().ToString() + ".csv");

            var result = service.ImportFromCsv(path);

            Assert.Equal(0, result.TotalRows);
            Assert.Equal(0, result.ImportedCount);
            Assert.Single(result.Errors);
            Assert.Contains("Файл не найден", result.Errors[0].Message);
        }

        // Тест 4: Пустой файл
        [Fact]
        public void ImportFromCsv_EmptyFile_ReturnsError()
        {
            var service = GetService(out _);
            var path = Path.Combine(Path.GetTempPath(), System.Guid.NewGuid().ToString() + ".csv");
            File.WriteAllText(path, string.Empty);

            var result = service.ImportFromCsv(path);

            Assert.Equal(0, result.TotalRows);
            Assert.Equal(0, result.ImportedCount);
            Assert.Single(result.Errors);
            Assert.Contains("Данные для импорта отсутствуют", result.Errors[0].Message);

            File.Delete(path);
        }

        // Тест 5: Только заголовок без данных
        [Fact]
        public void ImportFromLines_OnlyHeader_ReturnsError()
        {
            var service = GetService(out _);
            var lines = new[] { Header };

            var result = service.ImportFromLines(lines);

            Assert.Equal(0, result.TotalRows);
            Assert.Equal(0, result.ImportedCount);
            Assert.Single(result.Errors);
            Assert.Contains("Данные для импорта отсутствуют", result.Errors[0].Message);
        }

        // Тест 6: Неверный заголовок CSV
        [Fact]
        public void ImportFromLines_InvalidHeader_ReturnsError()
        {
            var service = GetService(out _);
            var lines = new[] { "sku,name,description,price,currency,category_name,stock,image_url,is_active" };

            var result = service.ImportFromLines(lines);

            Assert.Equal(0, result.TotalRows);
            Assert.Equal(0, result.ImportedCount);
            Assert.Single(result.Errors);
            Assert.Contains("Неверные заголовки CSV", result.Errors[0].Message);
        }

        // Тест 7: Неверное количество полей
        [Fact]
        public void ImportFromLines_WrongFieldCount_ReturnsError()
        {
            var service = GetService(out _);
            var lines = new[] { Header, "1001,Ноутбук Lenovo IdeaPad 3" };

            var result = service.ImportFromLines(lines);

            Assert.Equal(1, result.TotalRows);
            Assert.Equal(0, result.ImportedCount);
            Assert.Single(result.Errors);
            Assert.Contains("Неверное количество полей в строке", result.Errors[0].Message);
        }

        // Тест 8: Пустой SKU
        [Fact]
        public void ImportFromLines_EmptySku_ReturnsError()
        {
            var service = GetService(out _);
            var lines = new[] { Header, ",Ноутбук Lenovo IdeaPad 3,Бюджетный ноутбук,45990,RUB,Компьютеры,5,/images/laptop.jpg,true,Lenovo,24,IdeaPad 3-15" };

            var result = service.ImportFromLines(lines);

            Assert.Equal(0, result.ImportedCount);
            Assert.Single(result.Errors);
            Assert.Contains("Поле sku не может быть пустым", result.Errors[0].Message);
        }

        // Тест 9: Пустое название товара
        [Fact]
        public void ImportFromLines_EmptyName_ReturnsError()
        {
            var service = GetService(out _);
            var lines = new[] { Header, "1001,,Бюджетный ноутбук,45990,RUB,Компьютеры,5,/images/laptop.jpg,true,Lenovo,24,IdeaPad 3-15" };

            var result = service.ImportFromLines(lines);

            Assert.Equal(0, result.ImportedCount);
            Assert.Single(result.Errors);
            Assert.Contains("Поле name не может быть пустым", result.Errors[0].Message);
        }

        // Тест 10: Неверная цена — отрицательное значение
        [Fact]
        public void ImportFromLines_NegativePrice_ReturnsError()
        {
            var service = GetService(out _);
            var lines = new[] { Header, "1003,Клавиатура A4Tech Bloody V3M,Механическая игровая клавиатура,-500,RUB,Периферия,7,/images/keyboard.jpg,true,A4Tech,24,V3M" };

            var result = service.ImportFromLines(lines);

            Assert.Equal(0, result.ImportedCount);
            Assert.Single(result.Errors);
            Assert.Contains("Поле price должно быть числом больше 0", result.Errors[0].Message);
        }

        // Тест 11: Неверная цена — нечисловое значение
        [Fact]
        public void ImportFromLines_NonNumericPrice_ReturnsError()
        {
            var service = GetService(out _);
            var lines = new[] { Header, "1003,Клавиатура A4Tech Bloody V3M,Механическая игровая клавиатура,abc,RUB,Периферия,7,/images/keyboard.jpg,true,A4Tech,24,V3M" };

            var result = service.ImportFromLines(lines);

            Assert.Equal(0, result.ImportedCount);
            Assert.Single(result.Errors);
            Assert.Contains("Поле price должно быть числом больше 0", result.Errors[0].Message);
        }

        // Тест 12: Отрицательный остаток
        [Fact]
        public void ImportFromLines_NegativeStock_ReturnsError()
        {
            var service = GetService(out _);
            var lines = new[] { Header, "1004,Монитор Dell S2722QC,Игровой монитор 27 дюймов,29990,RUB,Компьютеры,-5,/images/monitor.jpg,true,Dell,24,S2722QC" };

            var result = service.ImportFromLines(lines);

            Assert.Equal(0, result.ImportedCount);
            Assert.Single(result.Errors);
            Assert.Contains("Остаток на складе должен быть целым числом не меньше 0", result.Errors[0].Message);
        }

        // Тест 13: Неверное значение is_active
        [Fact]
        public void ImportFromLines_InvalidIsActive_ReturnsError()
        {
            var service = GetService(out _);
            var lines = new[] { Header, "1007,Webcam Logitech C920,Веб-камера Full HD,4990,RUB,Периферия,6,/images/webcam.jpg,yes,Logitech,24,C920" };

            var result = service.ImportFromLines(lines);

            Assert.Equal(0, result.ImportedCount);
            Assert.Single(result.Errors);
            Assert.Contains("Поле is_active должно иметь значение true, false, 1 или 0", result.Errors[0].Message);
        }

        // Тест 14: Отсутствует бренд
        [Fact]
        public void ImportFromLines_MissingBrand_ReturnsError()
        {
            var service = GetService(out _);
            var lines = new[] { Header, "1005,Видеокарта NVIDIA GeForce RTX 3060,,24990,RUB,Компьютеры,4,/images/gpu.jpg,true,,36,RTX3060" };

            var result = service.ImportFromLines(lines);

            Assert.Equal(0, result.ImportedCount);
            Assert.Single(result.Errors);
            Assert.Contains("Поле brand обязательно для варианта «Электроника»", result.Errors[0].Message);
        }

        // Тест 15: Гарантия меньше нуля
        [Fact]
        public void ImportFromLines_WarrantyLessThanZero_ReturnsError()
        {
            var service = GetService(out _);
            var lines = new[] { Header, "1006,Накопитель SSD Samsung 870 EVO 1TB,Внутренний твердотельный накопитель,5990,RUB,Компьютеры,12,/images/ssd.jpg,true,Samsung,-1,870EVO-1T" };

            var result = service.ImportFromLines(lines);

            Assert.Equal(0, result.ImportedCount);
            Assert.Single(result.Errors);
            Assert.Contains("Гарантия должна быть целым числом от 0 до 60 месяцев", result.Errors[0].Message);
        }

        // Тест 16: Гарантия больше 60
        [Fact]
        public void ImportFromLines_WarrantyMoreThan60_ReturnsError()
        {
            var service = GetService(out _);
            var lines = new[] { Header, "1006,Накопитель SSD Samsung 870 EVO 1TB,Внутренний твердотельный накопитель,5990,RUB,Компьютеры,12,/images/ssd.jpg,true,Samsung,61,870EVO-1T" };

            var result = service.ImportFromLines(lines);

            Assert.Equal(0, result.ImportedCount);
            Assert.Single(result.Errors);
            Assert.Contains("Гарантия должна быть целым числом от 0 до 60 месяцев", result.Errors[0].Message);
        }

        // Тест 17: Дубликат SKU внутри одного файла
        [Fact]
        public void ImportFromLines_DuplicateSkuInFile_ReturnsError()
        {
            var service = GetService(out var repo);
            var lines = new[] {
                Header,
                "1001,Ноутбук Lenovo IdeaPad 3,Бюджетный ноутбук для работы и учебы,45990,RUB,Компьютеры,5,/images/laptop.jpg,true,Lenovo,24,IdeaPad 3-15",
                "1001,Ноутбук ASUS VivoBook,Другой ноутбук,52990,RUB,Компьютеры,7,/images/asus.jpg,true,ASUS,24,VivoBook"
            };

            var result = service.ImportFromLines(lines);

            Assert.Equal(2, result.TotalRows);
            Assert.Equal(1, result.ImportedCount);
            Assert.Single(result.Errors);
            Assert.Contains("Артикул товара уже существует", result.Errors[0].Message);

            var product = repo.GetBySku("1001");
            Assert.NotNull(product);
            Assert.Equal("Ноутбук Lenovo IdeaPad 3", product.Name);
        }

        // Тест 18: Дубликат SKU с уже существующим товаром в репозитории
        [Fact]
        public void ImportFromLines_DuplicateSkuInRepository_ReturnsError()
        {
            var service = GetService(out var repo);
            repo.Add(new Product { Sku = "1001", Name = "Старый товар" });

            var lines = new[] { Header, "1001,Ноутбук ASUS VivoBook,Другой ноутбук,52990,RUB,Компьютеры,7,/images/asus.jpg,true,ASUS,24,VivoBook" };

            var result = service.ImportFromLines(lines);

            Assert.Equal(0, result.ImportedCount);
            Assert.Single(result.Errors);
            Assert.Contains("Артикул товара уже существует", result.Errors[0].Message);
            Assert.Equal(1, repo.Count); 
        }

        // Тест 19: Несколько ошибок в одной строке
        [Fact]
        public void ImportFromLines_MultipleErrorsInOneRow_ReturnsAllErrors()
        {
            var service = GetService(out var repo);

            var lines = new[] { Header, ",,Описание,-10,RUB,Компьютеры,-5,/images/x.jpg,yes,Brand,12,Model" };

            var result = service.ImportFromLines(lines);

            Assert.Equal(0, result.ImportedCount);
            Assert.True(result.Errors.Count >= 4); 
            Assert.Equal(0, repo.Count);
        }

        // Тест 20: Пустые необязательные поля разрешены
        [Fact]
        public void ImportFromLines_EmptyOptionalFields_AddsProduct()
        {
            var service = GetService(out var repo);
            var lines = new[] { Header, "1011,Блок питания Corsair CV550,,4990,RUB,Компьютеры,9,,true,Corsair,60,CV550" };

            var result = service.ImportFromLines(lines);

            Assert.Equal(1, result.ImportedCount);
            Assert.Empty(result.Errors);

            var product = repo.GetBySku("1011");
            Assert.Equal(string.Empty, product.Description);
            Assert.Equal(string.Empty, product.ImageUrl);
        }

        // Тест 21: Пустая валюта заменяется на RUB
        [Fact]
        public void ImportFromLines_EmptyCurrency_DefaultsToRub()
        {
            var service = GetService(out var repo);
            var lines = new[] { Header, "1011,Блок питания Corsair CV550,,4990,,Компьютеры,9,,true,Corsair,60,CV550" };

            var result = service.ImportFromLines(lines);

            Assert.Equal(1, result.ImportedCount);
            var product = repo.GetBySku("1011");
            Assert.Equal("RUB", product.Currency);
        }

        // Тест 22: Пустая категория заменяется на «Без категории»
        [Fact]
        public void ImportFromLines_EmptyCategory_DefaultsToDefaultCategory()
        {
            var service = GetService(out var repo);
            var lines = new[] { Header, "1011,Блок питания Corsair CV550,,4990,RUB,,9,,true,Corsair,60,CV550" };

            var result = service.ImportFromLines(lines);

            Assert.Equal(1, result.ImportedCount);
            var product = repo.GetBySku("1011");
            Assert.Equal("Без категории", product.CategoryName);
        }

        // Тест 23: Кириллица сохраняется корректно
        [Fact]
        public void ImportFromLines_Cyrillic_IsPreserved()
        {
            var service = GetService(out var repo);
            var lines = new[] { Header, "1001,Ноутбук Lenovo IdeaPad 3,Бюджетный ноутбук для работы и учебы,45990,RUB,Компьютеры,5,/images/laptop.jpg,true,Lenovo,24,IdeaPad 3-15" };

            var result = service.ImportFromLines(lines);

            var product = repo.GetBySku("1001");
            Assert.Equal("Ноутбук Lenovo IdeaPad 3", product.Name);
            Assert.Equal("Бюджетный ноутбук для работы и учебы", product.Description);
        }
    }
}
