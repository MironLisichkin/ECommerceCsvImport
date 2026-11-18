# ECommerceCsvImport

ASP.NET Core MVC (.NET 8): импорт товаров из CSV в память, вариант 1 «Электроника» (ПП.02.01).

## Структура репозитория
```
ECommerceCsvImport.sln
ECommerceCsvImport/          веб-проект (MVC)
ECommerceCsvImport.Tests/    xUnit-тесты + testdata/*.csv
```

## Запуск
```
dotnet run --project ECommerceCsvImport
dotnet test
```
Страница импорта: `/Products/Import`, путь к файлу: `/csv/sample_products.csv`
