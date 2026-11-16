
using System.Data.Entity;
using MyStore.Models;
using MyStore.Models.ViewModel;
using System;
using System.Linq;
using System.Web.Mvc;

namespace MyStore.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly MyStoreEntities db = new MyStoreEntities();

        private static readonly string[] Statuses =
        {
            "Chờ thanh toán",
            "Đang xử lý",
            "Đang vận chuyển",
            "Đã giao",
            "Đã hủy"
        };

        public ActionResult Checkout()
        {
            var cart = new CartService(Session).GetCart();

            if (!cart.Items.Any())
                return RedirectToAction("Index2", "Cart");

            var customer = db.Customers.SingleOrDefault(
                c => c.Username == User.Identity.Name
            );

            if (customer == null)
                return RedirectToAction("Login", "Account");

            return View(new CheckoutVM
            {
                CartItems = cart.Items.ToList(),
                TotalAmount = cart.TotalValue(),
                OrderDate = DateTime.Now,
                ShippingAddress = customer.CustomerAddress,
                CustomerID = customer.CustomerID,
                CustomerName = customer.CustomerName,
                CustomerPhone = customer.CustomerPhone,
                CustomerEmail = customer.CustomerEmail,
                Username = customer.Username,
                PaymentMethod = "Tiền mặt"
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Checkout(CheckoutVM model)
        {
            var cart = new CartService(Session).GetCart();

            if (!cart.Items.Any())
                return RedirectToAction("Index2", "Cart");

            var customer = db.Customers.SingleOrDefault(
                c => c.Username == User.Identity.Name
            );

            if (customer == null)
                return RedirectToAction("Login", "Account");

            // Luôn sử dụng thanh toán khi nhận hàng.
            model.PaymentMethod = "Tiền mặt";

            if (String.IsNullOrWhiteSpace(model.ShippingAddress))
            {
                ModelState.AddModelError(
                    "ShippingAddress",
                    "Vui lòng nhập địa chỉ giao hàng."
                );
            }

            if (String.IsNullOrWhiteSpace(model.ShippingMethod) ||
                !new[] { "Giao hàng nhanh", "Giao hàng tiết kiệm" }.Contains(model.ShippingMethod))
            {
                ModelState.AddModelError(
                    "ShippingMethod",
                    "Vui lòng chọn phương thức giao hàng hợp lệ."
                );
            }

            if (!ModelState.IsValid)
            {
                model.CartItems = cart.Items.ToList();
                model.TotalAmount = cart.TotalValue();
                model.CustomerName = customer.CustomerName;
                model.CustomerPhone = customer.CustomerPhone;
                model.CustomerEmail = customer.CustomerEmail;

                return View(model);
            }

            using (var tx = db.Database.BeginTransaction())
            {
                var order = new Order
                {
                    CustomerID = customer.CustomerID,
                    OrderDate = DateTime.Now,
                    ShippingAddress = model.ShippingAddress.Trim(),
                    PaymentMethod = "Tiền mặt",
                    ShippingMethod = model.ShippingMethod,
                    PaymentStatus = "Thanh toán tiền mặt",
                    OrderStatus = "Đang xử lý"
                };

                decimal total = 0;

                foreach (var item in cart.Items)
                {
                    var product = db.Products.SingleOrDefault(
                        p => p.ProductID == item.ProductID
                    );

                    if (product == null ||
                        item.Quantity < 1 ||
                        product.Quantity < item.Quantity)
                    {
                        tx.Rollback();

                        TempData["Error"] =
                            "Sản phẩm " + item.ProductName +
                            " không đủ tồn kho. Vui lòng cập nhật giỏ hàng.";

                        return RedirectToAction("Index2", "Cart");
                    }

                    product.Quantity -= item.Quantity;
                    product.QuantitySold += item.Quantity;

                    order.OrderDetails.Add(new OrderDetail
                    {
                        ProductID = product.ProductID,
                        Quantity = item.Quantity,
                        UnitPrice = product.ProductPrice
                    });

                    total += product.ProductPrice * item.Quantity;
                }

                order.TotalAmount = total;

                db.Orders.Add(order);
                db.SaveChanges();

                tx.Commit();

                new CartService(Session).ClearCart();

                return RedirectToAction(
                    "OrderSuccess",
                    new { id = order.OrderID }
                );
            }
        }

        public ActionResult OrderSuccess(int id)
        {
            var customer = db.Customers.SingleOrDefault(
                c => c.Username == User.Identity.Name
            );

            if (customer == null)
                return HttpNotFound();

            var order = db.Orders
                .Include("OrderDetails.Product")
                .Include("Customer")
                .SingleOrDefault(
                    o => o.OrderID == id &&
                         o.CustomerID == customer.CustomerID
                );

            if (order == null)
                return HttpNotFound();

            return View(order);
        }

        public ActionResult OrderHistory(
            string searchTerm,
            string status,
            bool more = false)
        {
            var customer = db.Customers.SingleOrDefault(
                c => c.Username == User.Identity.Name
            );

            if (customer == null)
                return RedirectToAction("Login", "Account");

            var query = db.Orders
                .Include("OrderDetails.Product")
                .Where(o => o.CustomerID == customer.CustomerID);

            if (!String.IsNullOrWhiteSpace(searchTerm))
            {
                int orderId;

                if (Int32.TryParse(searchTerm, out orderId))
                {
                    query = query.Where(o =>
                        o.OrderID == orderId ||
                        o.OrderDetails.Any(d =>
                            d.Product.ProductName.Contains(searchTerm)
                        )
                    );
                }
                else
                {
                    query = query.Where(o =>
                        o.OrderDetails.Any(d =>
                            d.Product.ProductName.Contains(searchTerm)
                        )
                    );
                }
            }

            if (!String.IsNullOrWhiteSpace(status) && status != "Tất cả")
            {
                query = query.Where(o => o.OrderStatus == status);
            }

            var all = query
                .OrderByDescending(o => o.OrderDate)
                .ThenByDescending(o => o.OrderID)
                .ToList();

            var groups = new System.Collections.Generic.List<OrderHistoryGroupVM>();

            var groupStatuses =
                String.IsNullOrWhiteSpace(status) || status == "Tất cả"
                ? Statuses
                : new[] { status };

            foreach (var itemStatus in groupStatuses)
            {
                var matching = all
                    .Where(o => o.OrderStatus == itemStatus)
                    .OrderByDescending(o => o.OrderDate)
                    .ToList();

                if (matching.Count > 0)
                {
                    groups.Add(new OrderHistoryGroupVM
                    {
                        Status = itemStatus,
                        TotalCount = matching.Count,
                        Orders = matching.Take(more ? 50 : 2).ToList()
                    });
                }
            }

            return View("MyOrder", new OrderHistoryVM
            {
                SearchTerm = searchTerm,
                Status = status ?? "Tất cả",
                ShowMore = more,
                HasAnyOrders = db.Orders.Any(
                    o => o.CustomerID == customer.CustomerID
                ),
                Groups = groups
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CancelOrder(int id)
        {
            var customer = db.Customers.SingleOrDefault(
                c => c.Username == User.Identity.Name
            );

            if (customer == null)
                return RedirectToAction("Login", "Account");

            using (var tx = db.Database.BeginTransaction())
            {
                var order = db.Orders
                    .Include("OrderDetails.Product")
                    .SingleOrDefault(
                        o => o.OrderID == id &&
                             o.CustomerID == customer.CustomerID
                    );

                if (order == null)
                {
                    tx.Rollback();
                    TempData["Error"] = "Không tìm thấy đơn hàng.";
                    return RedirectToAction("OrderHistory");
                }

                if (order.OrderStatus != "Chờ thanh toán" &&
                    order.OrderStatus != "Đang xử lý")
                {
                    tx.Rollback();

                    TempData["Error"] =
                        "Đơn hàng hiện tại không thể hủy.";

                    return RedirectToAction("OrderHistory");
                }

                foreach (var detail in order.OrderDetails)
                {
                    var product = detail.Product;

                    if (product != null)
                    {
                        // Hoàn số lượng sản phẩm về kho.
                        product.Quantity += detail.Quantity;

                        // Cập nhật lại số lượng đã bán.
                        product.QuantitySold -= detail.Quantity;

                        if (product.QuantitySold < 0)
                            product.QuantitySold = 0;
                    }
                }

                order.OrderStatus = "Đã hủy";

                db.SaveChanges();
                tx.Commit();
            }

            TempData["Success"] = "Đã hủy đơn hàng thành công.";

            return RedirectToAction("OrderHistory");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmReceived(int id)
        {
            var customer = db.Customers.SingleOrDefault(
                c => c.Username == User.Identity.Name
            );

            if (customer == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var order = db.Orders.SingleOrDefault(
                o => o.OrderID == id &&
                     o.CustomerID == customer.CustomerID
            );

            if (order == null)
            {
                return HttpNotFound();
            }

            if (order.OrderStatus != "Đang vận chuyển")
            {
                TempData["Error"] = "Đơn hàng không thể xác nhận ở trạng thái hiện tại.";
                return RedirectToAction("OrderHistory");
            }

            order.OrderStatus = "Đã giao";
            order.PaymentStatus = "Đã thanh toán";

            db.SaveChanges();

            TempData["Success"] = "Bạn đã xác nhận nhận hàng thành công.";

            return RedirectToAction("OrderHistory");
        }

        public ActionResult OrderDetail(int id)
        {
            var customer = db.Customers.SingleOrDefault(
                c => c.Username == User.Identity.Name
            );

            if (customer == null)
                return HttpNotFound();

            var order = db.Orders
                .Include("Customer")
                .Include("OrderDetails.Product")
                .SingleOrDefault(
                    o => o.OrderID == id &&
                         o.CustomerID == customer.CustomerID
                );

            return order == null
                ? (ActionResult)HttpNotFound()
                : View(order);
        }

        public ActionResult Reorder(int id)
        {
            var customer = db.Customers.SingleOrDefault(
                c => c.Username == User.Identity.Name
            );

            if (customer == null)
                return HttpNotFound();

            var order = db.Orders
                .Include("OrderDetails.Product.Category")
                .SingleOrDefault(
                    o => o.OrderID == id &&
                         o.CustomerID == customer.CustomerID
                );

            if (order == null)
                return HttpNotFound();

            var cart = new CartService(Session).GetCart();

            foreach (var detail in order.OrderDetails)
            {
                var product = detail.Product;

                if (product != null && product.Quantity > 0)
                {
                    var existingItem = cart.Items.FirstOrDefault(
                        i => i.ProductID == product.ProductID
                    );

                    if (existingItem == null ||
                        existingItem.Quantity < product.Quantity)
                    {
                        cart.AddItem(
                            product.ProductID,
                            product.ProductImage,
                            product.ProductName,
                            product.ProductPrice,
                            1,
                            product.Category.CategoryName,
                            product.Quantity
                        );
                    }
                }
            }

            return RedirectToAction("Index2", "Cart");
        }

        public ActionResult MyOrder(
            string searchTerm,
            string status,
            bool more = false)
        {
            return OrderHistory(searchTerm, status, more);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();

            base.Dispose(disposing);
        }
    }
}
