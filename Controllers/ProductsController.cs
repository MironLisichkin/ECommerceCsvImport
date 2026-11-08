using Microsoft.AspNetCore.Mvc;
using ECommerceCsvImport.Models;
using ECommerceCsvImport.Services;

namespace ECommerceCsvImport.Controllers
{
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
                return View("Import", new ImportViewModel { FilePath = filePath });
            }

            ViewBag.DebugPath = resolvedPath;

            var result = _importService.ImportFromCsv(resolvedPath);

            var viewModel = new ImportViewModel
            {
                FilePath = filePath,
                Result = result
            };

            return View(viewModel);
        }

        private string ResolveFilePath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return string.Empty;

            var cleanedPath = filePath.TrimStart('/', '\\');
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

            var pathInBin = Path.Combine(baseDirectory, "wwwroot", cleanedPath);

            var projectDir = Path.GetFullPath(Path.Combine(baseDirectory, @"..\..\..\"));
            var pathInProject = Path.Combine(projectDir, "wwwroot", cleanedPath);

            if (System.IO.File.Exists(pathInBin)) return pathInBin;
            if (System.IO.File.Exists(pathInProject)) return pathInProject;

            return pathInProject;
        }
    }
}
