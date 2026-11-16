
using System.Linq;
using System.Web.Mvc;
using MyStore.Infrastructure;
using MyStore.Models;

namespace MyStore.Areas.admin.Controllers
{
    [AdminAuthorize]
    public class HomeController : Controller
    {
        private readonly MyStoreEntities db = new MyStoreEntities();

        // GET: admin/Home
        // Trang thống kê tổng quan
        public ActionResult Index()
        {
            // Tổng số sản phẩm
            ViewBag.ProductCount = db.Products
                .AsNoTracking()
                .Count();

            // Tổng số danh mục
            ViewBag.CategoryCount = db.Categories
                .AsNoTracking()
                .Count();

            // Tổng số khách hàng
            ViewBag.CustomerCount = db.Customers
                .AsNoTracking()
                .Count();

            // Tổng số đơn hàng
            ViewBag.OrderCount = db.Orders
                .AsNoTracking()
                .Count();

            // Số sản phẩm đã hết hàng
            ViewBag.OutOfStock = db.Products
                .AsNoTracking()
                .Count(p => p.Quantity == 0);

            // Tổng doanh thu từ các đơn hàng đã giao
            ViewBag.TotalRevenue = db.Orders
                .AsNoTracking()
                .Where(o => o.OrderStatus == "Đã giao")
                .Select(o => (decimal?)o.TotalAmount)
                .Sum() ?? 0;

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
