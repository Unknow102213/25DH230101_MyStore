
using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using MyStore.Infrastructure;
using MyStore.Models;
using MyStore.Models.ViewModel;
using PagedList;

namespace MyStore.Areas.admin.Controllers
{
    [AdminAuthorize]
    public class OrdersController : Controller
    {
        private readonly MyStoreEntities db = new MyStoreEntities();

        // Danh sách trạng thái đơn hàng
        private static readonly string[] Statuses =
        {
            "Chờ thanh toán",
            "Đang xử lý",
            "Đang vận chuyển",
            "Đã giao",
            "Đã hủy"
        };

        // Danh sách trạng thái thanh toán
        private static readonly string[] PaymentStatuses =
        {
            "Chưa thanh toán",
            "Thanh toán tiền mặt",
            "Thanh toán paypal",
            "Đã thanh toán",
            "Đã hoàn tiền"
        };

        // GET: admin/Orders
        // Danh sách đơn hàng, tìm kiếm và phân trang
        public ActionResult Index(
            string customerName,
            string paymentStatus,
            DateTime? orderDate,
            int? page)
        {
            var orders = db.Orders
                .AsNoTracking()
                .Include("Customer")
                .AsQueryable();

            // Tìm kiếm theo tên khách hàng
            if (!string.IsNullOrWhiteSpace(customerName))
            {
                customerName = customerName.Trim();

                orders = orders.Where(o =>
                    o.Customer.CustomerName != null &&
                    o.Customer.CustomerName.Contains(customerName)
                );
            }

            // Lọc theo trạng thái thanh toán
            if (!string.IsNullOrWhiteSpace(paymentStatus))
            {
                orders = orders.Where(o =>
                    o.PaymentStatus.Contains(paymentStatus)
                );
            }

            // Lọc theo ngày đặt hàng
            if (orderDate.HasValue)
            {
                DateTime startDate = orderDate.Value.Date;
                DateTime endDate = startDate.AddDays(1);

                orders = orders.Where(o =>
                    o.OrderDate >= startDate &&
                    o.OrderDate < endDate
                );
            }

            // Tạo ViewModel
            var model = new AdminOrderSearchVM
            {
                CustomerName = customerName,
                PaymentStatus = paymentStatus,
                OrderDate = orderDate,

                Orders = orders
                    .OrderByDescending(o => o.OrderDate)
                    .ThenByDescending(o => o.OrderID)
                    .ToPagedList(page ?? 1, 15)
            };

            ViewBag.PaymentStatuses = new SelectList(
                PaymentStatuses,
                paymentStatus
            );

            return View(model);
        }

        // GET: admin/Orders/Details/5
        // Xem chi tiết đơn hàng
        public ActionResult Details(int? id)
        {
            if (!id.HasValue || id.Value <= 0)
            {
                return new HttpStatusCodeResult(
                    HttpStatusCode.BadRequest
                );
            }

            var order = db.Orders
                .AsNoTracking()
                .Include("Customer")
                .Include("OrderDetails.Product")
                .SingleOrDefault(o => o.OrderID == id.Value);

            if (order == null)
            {
                return HttpNotFound();
            }

            return View(order);
        }

        // GET: admin/Orders/UpdateStatus/5
        // Hiển thị trang cập nhật trạng thái
        public ActionResult UpdateStatus(int? id)
        {
            if (!id.HasValue || id.Value <= 0)
            {
                return new HttpStatusCodeResult(
                    HttpStatusCode.BadRequest
                );
            }

            var order = db.Orders
                .Include("Customer")
                .SingleOrDefault(o => o.OrderID == id.Value);

            if (order == null)
            {
                return HttpNotFound();
            }

            ViewBag.Statuses = new SelectList(
                Statuses,
                order.OrderStatus
            );

            return View(order);
        }

        // POST: admin/Orders/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateStatus(int id, string orderStatus)
        {
            var order = db.Orders
                .Include("Customer")
                .SingleOrDefault(o => o.OrderID == id);

            if (order == null)
            {
                return HttpNotFound();
            }

            // Kiểm tra trạng thái hợp lệ
            if (!Statuses.Contains(orderStatus))
            {
                ModelState.AddModelError(
                    "",
                    "Trạng thái đơn hàng không hợp lệ."
                );

                ViewBag.Statuses = new SelectList(
                    Statuses,
                    order.OrderStatus
                );

                return View(order);
            }

            string oldStatus = order.OrderStatus;

            // Không cho cập nhật lại cùng trạng thái
            if (oldStatus == orderStatus)
            {
                TempData["Error"] = "Đơn hàng đang ở trạng thái này.";

                return RedirectToAction(
                    "Details",
                    new { id = id }
                );
            }

            // Kiểm tra quy trình chuyển trạng thái
            bool validTransition =
                (oldStatus == "Chờ thanh toán" &&
                    (orderStatus == "Đang xử lý" ||
                     orderStatus == "Đã hủy"))
                ||
                (oldStatus == "Đang xử lý" &&
                    (orderStatus == "Đang vận chuyển" ||
                     orderStatus == "Đã hủy"))
                ||
                (oldStatus == "Đang vận chuyển" &&
                    (orderStatus == "Đã giao" ||
                     orderStatus == "Đã hủy"));

            if (!validTransition)
            {
                TempData["Error"] =
                    "Không thể chuyển sang trạng thái được chọn.";

                return RedirectToAction(
                    "Details",
                    new { id = id }
                );
            }

            using (var tx = db.Database.BeginTransaction())
            {
                try
                {
                    var details = db.OrderDetails
                        .Where(d => d.OrderID == id)
                        .ToList();

                    // Hủy đơn hàng: hoàn lại số lượng tồn kho
                    if (orderStatus == "Đã hủy")
                    {
                        foreach (var detail in details)
                        {
                            var product = db.Products
                                .Find(detail.ProductID);

                            if (product != null)
                            {
                                product.Quantity += detail.Quantity;

                                product.QuantitySold = Math.Max(
                                    0,
                                    product.QuantitySold - detail.Quantity
                                );
                            }
                        }

                        // Hoàn tiền nếu đơn đã thanh toán
                        if (order.PaymentStatus == "Đã thanh toán")
                        {
                            order.PaymentStatus = "Đã hoàn tiền";
                        }
                    }

                    // Cập nhật trạng thái đơn hàng
                    order.OrderStatus = orderStatus;

                    // Cập nhật trạng thái thanh toán
                    if (orderStatus == "Đã giao" &&
                        order.PaymentStatus != "Đã thanh toán")
                    {
                        order.PaymentStatus = "Đã thanh toán";
                    }

                    db.SaveChanges();

                    tx.Commit();

                    TempData["Success"] =
                        "Đã cập nhật trạng thái đơn hàng thành công.";
                }
                catch
                {
                    tx.Rollback();

                    TempData["Error"] =
                        "Có lỗi xảy ra khi cập nhật đơn hàng.";
                }
            }

            return RedirectToAction(
                "Details",
                new { id = id }
            );
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
