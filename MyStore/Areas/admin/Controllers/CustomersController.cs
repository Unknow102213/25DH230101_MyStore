
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using MyStore.Infrastructure;
using MyStore.Models;

namespace MyStore.Areas.admin.Controllers
{
    [AdminAuthorize]
    public class CustomersController : Controller
    {
        private readonly MyStoreEntities db = new MyStoreEntities();

        // GET: admin/Customers
        // Hiển thị danh sách khách hàng và tìm kiếm
        public ActionResult Index(string searchTerm)
        {
            var customers = db.Customers
                .AsNoTracking()
                .AsQueryable();

            // Tìm kiếm theo tên, số điện thoại hoặc email
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.Trim();

                customers = customers.Where(c =>
                    (c.CustomerName != null &&
                     c.CustomerName.Contains(searchTerm))
                    ||
                    (c.CustomerPhone != null &&
                     c.CustomerPhone.Contains(searchTerm))
                    ||
                    (c.CustomerEmail != null &&
                     c.CustomerEmail.Contains(searchTerm))
                );
            }

            ViewBag.SearchTerm = searchTerm;

            var result = customers
                .OrderBy(c => c.CustomerName)
                .ToList();

            return View(result);
        }

        // GET: admin/Customers/Details/5
        // Hiển thị thông tin khách hàng và danh sách đơn hàng
        public ActionResult Details(int? id)
        {
            // Kiểm tra mã khách hàng
            if (!id.HasValue || id.Value <= 0)
            {
                return new HttpStatusCodeResult(
                    HttpStatusCode.BadRequest
                );
            }

            // Lấy thông tin khách hàng và các đơn hàng liên quan
            var customer = db.Customers
                .AsNoTracking()
                .Include("Orders.OrderDetails.Product")
                .Include("Orders.Customer")
                .SingleOrDefault(c => c.CustomerID == id.Value);

            // Không tìm thấy khách hàng
            if (customer == null)
            {
                return HttpNotFound();
            }

            return View(customer);
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
