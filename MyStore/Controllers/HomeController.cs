using System.Data.Entity;
using MyStore.Models;
using MyStore.Models.ViewModel;
using PagedList;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace MyStore.Controllers
{
    public class HomeController : Controller
    {
        private readonly MyStoreEntities db = new MyStoreEntities();

        public ActionResult Index(string searchTerm, int? page)
        {
            var products = db.Products.Include("Category").Where(p => p.Quantity > 0);
            if (!string.IsNullOrWhiteSpace(searchTerm))
                products = products.Where(p => p.ProductName.Contains(searchTerm) || p.ProductDescription.Contains(searchTerm) || p.Category.CategoryName.Contains(searchTerm));
            var model = new HomeProductVM
            {
                SearchTerm = searchTerm,
                FeaturedProducts = products.OrderByDescending(p => p.QuantitySold).ThenBy(p => p.ProductName).Take(10).ToList(),
                NewProducts = products.OrderByDescending(p => p.ProductID).Take(20).ToPagedList(page ?? 1, 6)
            };
            return View(model);
        }

        public ActionResult Category(int? id, string searchTerm, decimal? minPrice, decimal? maxPrice, string sortOrder, int? page)
        {
            if (!id.HasValue) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var category = db.Categories.Find(id.Value);
            if (category == null) return HttpNotFound();
            var products = db.Products.Include("Category").Where(p => p.CategoryID == id.Value && p.Quantity > 0);
            if (!string.IsNullOrWhiteSpace(searchTerm)) products = products.Where(p => p.ProductName.Contains(searchTerm) || p.ProductDescription.Contains(searchTerm));
            if (minPrice.HasValue) products = products.Where(p => p.ProductPrice >= minPrice.Value);
            if (maxPrice.HasValue) products = products.Where(p => p.ProductPrice <= maxPrice.Value);
            switch (sortOrder)
            {
                case "name_asc": products = products.OrderBy(p => p.ProductName); break;
                case "name_desc": products = products.OrderByDescending(p => p.ProductName); break;
                case "price_asc": products = products.OrderBy(p => p.ProductPrice); break;
                case "price_desc": products = products.OrderByDescending(p => p.ProductPrice); break;
                default: products = products.OrderBy(p => p.ProductID); break;
            }
            var model = new ProductSearchVM { CategoryID = id.Value, CategoryName = category.CategoryName, SearchTerm = searchTerm, MinPrice = minPrice, MaxPrice = maxPrice, SortOrder = sortOrder, Products = products.ToPagedList(page ?? 1, 12) };
            return View(model);
        }

        public ActionResult ProductDetails(int? id, int? quantity, int? page)
        {
            if (!id.HasValue) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var product = db.Products.Include("Category").SingleOrDefault(p => p.ProductID == id.Value);
            if (product == null) return HttpNotFound();
            var related = db.Products.Include("Category").Where(p => p.CategoryID == product.CategoryID && p.ProductID != product.ProductID && p.Quantity > 0);
            var model = new ProductDetailsVM { product = product, quantity = quantity.HasValue && quantity.Value > 0 ? quantity.Value : 1,
                RelatedProducts = related.OrderBy(p => p.ProductName).Take(8).ToPagedList(page ?? 1, 3),
                TopProducts = db.Products.Where(p => p.Quantity > 0).OrderByDescending(p => p.QuantitySold).Take(8).ToPagedList(1, 8) };
            model.estimatedValue = product.ProductPrice * model.quantity;
            return View(model);
        }

        protected override void Dispose(bool disposing) { if (disposing) db.Dispose(); base.Dispose(disposing); }
    }
}
