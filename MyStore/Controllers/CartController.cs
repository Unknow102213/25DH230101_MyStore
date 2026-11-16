using System.Data.Entity;
using MyStore.Models;
using MyStore.Models.ViewModel;
using PagedList;
using System.Linq;
using System.Web.Mvc;

namespace MyStore.Controllers
{
    public class CartController : Controller
    {
        private readonly MyStoreEntities db = new MyStoreEntities();
        private CartService GetCartService() { return new CartService(Session); }

        public ActionResult Index2(int? page)
        {
            var cart = GetCartService().GetCart();
            var ids = cart.Items.Select(i => i.ProductID).ToList();
            var categories = cart.Items.Select(i => i.Category).Distinct().ToList();
            var similar = db.Products.Include("Category").Where(p => p.Quantity > 0 && categories.Contains(p.Category.CategoryName) && !ids.Contains(p.ProductID)).OrderBy(p => p.ProductName);
            cart.SimilarProducts = similar.ToPagedList(page ?? 1, cart.PageSize);
            return View(cart);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult AddToCart(int id, int quantity = 1)
        {
            var product = db.Products.Include("Category").SingleOrDefault(p => p.ProductID == id);
            if (product == null) return HttpNotFound();
            if (quantity < 1 || quantity > product.Quantity)
            {
                TempData["Error"] = "Số lượng mua không hợp lệ hoặc vượt quá tồn kho.";
                return RedirectToAction("ProductDetails", "Home", new { id = id });
            }
            var cart = GetCartService().GetCart();
            var existing = cart.Items.FirstOrDefault(i => i.ProductID == id);
            int requested = quantity + (existing == null ? 0 : existing.Quantity);
            if (requested > product.Quantity)
            {
                TempData["Error"] = "Số lượng trong giỏ vượt quá số lượng tồn kho.";
                return RedirectToAction("Index2");
            }
            cart.AddItem(product.ProductID, product.ProductImage, product.ProductName, product.ProductPrice, quantity, product.Category.CategoryName, product.Quantity);
            return RedirectToAction("Index2");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult RemoveFromCart(int id)
        {
            GetCartService().GetCart().RemoveItem(id);
            return RedirectToAction("Index2");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult ClearCart()
        {
            GetCartService().ClearCart();
            return RedirectToAction("Index2");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult UpdateQuantity(int id, int quantity)
        {
            var product = db.Products.Find(id);
            var cart = GetCartService().GetCart();
            if (product == null || quantity < 1 || quantity > product.Quantity)
                TempData["Error"] = "Số lượng phải từ 1 đến số lượng tồn kho.";
            else cart.UpdateQuantity(id, quantity);
            return RedirectToAction("Index2");
        }

        protected override void Dispose(bool disposing) { if (disposing) db.Dispose(); base.Dispose(disposing); }
    }
}
