using Microsoft.AspNetCore.Mvc;
using ECommerceCsvImport.Models;
using ECommerceCsvImport.Services;

namespace ECommerceCsvImport.Controllers
{
    /// <summary>
    /// Тонкий контроллер: вся логика импорта находится в IProductImportService.
    /// </summary>
    public class ProductsController : Controller
    {
        private readonly IProductRepository _repository;
        private readonly IProductImportService _importService;
        private readonly IWebHostEnvironment _environment;

        public ProductsController(
            IProductRepository repository,
            IProductImportService importService,
            IWebHostEnvironment environment)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _importService = importService ?? throw new ArgumentNullException(nameof(importService));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        }

        [HttpGet]
        public IActionResult Index()
        {
            var viewModel = new ProductsIndexViewModel
            {
                Products = _repository.GetAll()
            };
            return View(viewModel);
        }

        [HttpGet]
        public IActionResult Import()
        {
            return View(new ImportViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Import(string filePath)
        {
            var resolvedPath = ResolveFilePath(filePath);

            if (string.IsNullOrWhiteSpace(resolvedPath))
            {
                return View("Import", new ImportViewModel
                {
                    FilePath = filePath ?? string.Empty,
                    Result = new ImportResult
                    {
                        Errors = { new ImportError { RowNumber = 0, Message = "Укажите путь к CSV-файлу" } }
                    }
                });
            }

            var result = _importService.ImportFromCsv(resolvedPath);

            return View("Import", new ImportViewModel
            {
                FilePath = filePath ?? string.Empty,
                Result = result
            });
        }

        /// <summary>
        /// Поддерживает пути вида "/csv/sample_products.csv" (относительно wwwroot)
        /// и полные пути вида "C:\data\products.csv".
        /// </summary>
        private string? ResolveFilePath(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            var trimmed = input.Trim();

            // Полный путь с диском/UNC (C:\..., \\server\...) — используем как есть.
            if (Path.IsPathRooted(trimmed) && !trimmed.StartsWith('/') && !trimmed.StartsWith('\\'))
                return trimmed;

            // Иначе путь считается относительным к wwwroot.
            var relative = trimmed.TrimStart('/', '\\');
            return Path.Combine(_environment.WebRootPath, relative);
        }
    }
}
