using Xunit;
using ECommerceCsvImport.Models;
using ECommerceCsvImport.Services;

namespace ECommerceCsvImport.Tests
{
    public class RepositoryTests
    {
        // Тест 1: Добавление товара
        [Fact]
        public void Add_AddsProduct_GetAllContainsIt()
        {
            var repository = new InMemoryProductRepository();
            var product = new Product { Sku = "1001", Name = "Ноутбук Lenovo IdeaPad 3", Price = 45990m, Brand = "Lenovo" };

            repository.Add(product);

            var all = repository.GetAll();
            Assert.Single(all);
            Assert.Equal("1001", all[0].Sku);
            Assert.Equal(1, repository.Count);
        }

        // Тест 2: Поиск по SKU
        [Fact]
        public void GetBySku_ExistingSku_ReturnsProduct()
        {
            var repository = new InMemoryProductRepository();
            var product = new Product { Sku = "1001", Name = "Ноутбук" };
            repository.Add(product);

            var result = repository.GetBySku("1001");

            Assert.NotNull(result);
            Assert.Equal("1001", result.Sku);
        }

        // Тест 3: Поиск по SKU без учета регистра и пробелов
        [Fact]
        public void GetBySku_WithSpacesAndDifferentCase_ReturnsProduct()
        {
            var repository = new InMemoryProductRepository();
            var product = new Product { Sku = "1001", Name = "Ноутбук" };
            repository.Add(product);

            var result = repository.GetBySku(" 1001 ");

            Assert.NotNull(result);
            Assert.Equal("1001", result.Sku);
        }

        // Тест 4: Пустой репозиторий
        [Fact]
        public void EmptyRepository_ReturnsEmptyAndZeroCount()
        {
            var repository = new InMemoryProductRepository();

            Assert.Equal(0, repository.Count);
            Assert.Empty(repository.GetAll());
            Assert.Null(repository.GetBySku("1001"));
        }
    }
}