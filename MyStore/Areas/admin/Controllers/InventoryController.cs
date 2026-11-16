
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using MyStore.Infrastructure;
using MyStore.Models;

namespace MyStore.Areas.admin.Controllers
{
    [AdminAuthorize]
    public class InventoryController : Controller
    {
        private readonly MyStoreEntities db = new MyStoreEntities();

        // GET: admin/Inventory
        // Hiển thị danh sách sản phẩm trong kho
        public ActionResult Index()
        {
            // Danh sách sản phẩm còn hàng
            ViewBag.InStock = db.Products
                .AsNoTracking()
                .Include("Category")
                .Where(p => p.Quantity > 0)
                .OrderBy(p => p.ProductName)
                .ToList();

            // Danh sách sản phẩm đã hết hàng
            ViewBag.NeedRestock = db.Products
                .AsNoTracking()
                .Include("Category")
                .Where(p => p.Quantity == 0)
                .OrderBy(p => p.ProductName)
                .ToList();

            return View();
        }

        // Giải phóng kết nối Database
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
