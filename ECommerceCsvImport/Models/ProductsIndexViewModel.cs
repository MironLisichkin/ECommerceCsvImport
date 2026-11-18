namespace ECommerceCsvImport.Models
{
    public class ProductsIndexViewModel
    {
        public IReadOnlyList<Product> Products { get; set; } = new List<Product>();
        public int TotalCount => Products.Count;
        public decimal TotalValue => Products.Sum(p => p.Price);
    }
}