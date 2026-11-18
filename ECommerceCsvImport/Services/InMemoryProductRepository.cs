using ECommerceCsvImport.Models;

namespace ECommerceCsvImport.Services
{
    public class InMemoryProductRepository : IProductRepository
    {
        private readonly List<Product> _products = new();
        private readonly object _lock = new();

        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _products.Count;
                }
            }
        }

        public void Add(Product product)
        {
            if (product == null) throw new ArgumentNullException(nameof(product));

            lock (_lock)
            {
                _products.Add(product);
            }
        }

        public IReadOnlyList<Product> GetAll()
        {
            lock (_lock)
            {

                return _products.ToList().AsReadOnly();
            }
        }

        public Product? GetBySku(string sku)
        {
            if (string.IsNullOrWhiteSpace(sku)) return null;

            lock (_lock)
            {
                return _products.FirstOrDefault(p => string.Equals(p.Sku.Trim(), sku.Trim(), StringComparison.OrdinalIgnoreCase));
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _products.Clear();
            }
        }
    }
}