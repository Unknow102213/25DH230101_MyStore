
using System;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

using MyStore.Infrastructure;
using MyStore.Models;
using MyStore.Models.ViewModel;

using PagedList;

namespace MyStore.Areas.admin.Controllers
{
    [AdminAuthorize]
    public class ProductsController : Controller
    {
        private readonly MyStoreEntities db = new MyStoreEntities();

        public ActionResult Index(
            string SearchTerm,
            decimal? MinPrice,
            decimal? MaxPrice,
            string sortOrder,
            int? page)
        {
            var products = db.Products
                .Include("Category")
                .AsQueryable();

            if (!String.IsNullOrWhiteSpace(SearchTerm))
            {
                products = products.Where(p =>
                    p.ProductName.Contains(SearchTerm) ||
                    p.ProductDescription.Contains(SearchTerm) ||
                    p.Category.CategoryName.Contains(SearchTerm)
                );
            }

            if (MinPrice.HasValue)
            {
                products = products.Where(p =>
                    p.ProductPrice >= MinPrice.Value
                );
            }

            if (MaxPrice.HasValue)
            {
                products = products.Where(p =>
                    p.ProductPrice <= MaxPrice.Value
                );
            }

            switch (sortOrder)
            {
                case "name_asc":
                    products = products.OrderBy(p => p.ProductName);
                    break;

                case "name_desc":
                    products = products.OrderByDescending(p => p.ProductName);
                    break;

                case "price_asc":
                    products = products.OrderBy(p => p.ProductPrice);
                    break;

                case "price_desc":
                    products = products.OrderByDescending(p => p.ProductPrice);
                    break;

                default:
                    products = products.OrderBy(p => p.ProductID);
                    break;
            }

            var model = new ProductSearchVM
            {
                SearchTerm = SearchTerm,
                MinPrice = MinPrice,
                MaxPrice = MaxPrice,
                SortOrder = sortOrder,
                Products = products.ToPagedList(page ?? 1, 10)
            };

            return View(model);
        }

        public ActionResult Details(int? id)
        {
            if (!id.HasValue)
            {
                return new HttpStatusCodeResult(
                    HttpStatusCode.BadRequest
                );
            }

            var product = db.Products
                .Include("Category")
                .SingleOrDefault(p => p.ProductID == id.Value);

            return product == null
                ? (ActionResult)HttpNotFound()
                : View(product);
        }

        public ActionResult Create()
        {
            LoadCategories();

            return View(new Product
            {
                Quantity = 0,
                QuantitySold = 0
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Create(
            Product model,
            HttpPostedFileBase imageFile)
        {
            if (imageFile != null &&
                imageFile.ContentLength > 0 &&
                !IsImage(imageFile))
            {
                ModelState.AddModelError(
                    "",
                    "Chỉ chấp nhận tệp ảnh JPG, JPEG, PNG hoặc GIF."
                );
            }

            if (!db.Categories.Any(c =>
                c.CategoryID == model.CategoryID))
            {
                ModelState.AddModelError(
                    "CategoryID",
                    "Vui lòng chọn danh mục hợp lệ."
                );
            }

            if (ModelState.IsValid)
            {
                model.ProductImage = SaveImage(imageFile, null);
                model.QuantitySold = 0;

                db.Products.Add(model);
                db.SaveChanges();

                TempData["Success"] = "Đã thêm sản phẩm.";

                return RedirectToAction("Index");
            }

            LoadCategories(model.CategoryID);

            return View(model);
        }

        public ActionResult Edit(int? id)
        {
            if (!id.HasValue)
            {
                return new HttpStatusCodeResult(
                    HttpStatusCode.BadRequest
                );
            }

            var product = db.Products.Find(id.Value);

            if (product == null)
            {
                return HttpNotFound();
            }

            LoadCategories(product.CategoryID);

            return View(product);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Edit(
            Product model,
            HttpPostedFileBase imageFile)
        {
            var product = db.Products.Find(model.ProductID);

            if (product == null)
            {
                return HttpNotFound();
            }

            if (imageFile != null &&
                imageFile.ContentLength > 0 &&
                !IsImage(imageFile))
            {
                ModelState.AddModelError(
                    "",
                    "Chỉ chấp nhận tệp ảnh JPG, JPEG, PNG hoặc GIF."
                );
            }

            if (!db.Categories.Any(c =>
                c.CategoryID == model.CategoryID))
            {
                ModelState.AddModelError(
                    "CategoryID",
                    "Vui lòng chọn danh mục hợp lệ."
                );
            }

            if (!ModelState.IsValid)
            {
                LoadCategories(model.CategoryID);

                return View(model);
            }

            product.CategoryID = model.CategoryID;
            product.ProductName = model.ProductName;
            product.ProductPrice = model.ProductPrice;
            product.ProductDescription = model.ProductDescription;
            product.Quantity = model.Quantity;

            if (imageFile != null && imageFile.ContentLength > 0)
            {
                product.ProductImage = SaveImage(
                    imageFile,
                    product.ProductImage
                );
            }

            db.SaveChanges();

            TempData["Success"] = "Đã cập nhật sản phẩm.";

            return RedirectToAction("Index");
        }

        public ActionResult Delete(int? id)
        {
            if (!id.HasValue)
            {
                return new HttpStatusCodeResult(
                    HttpStatusCode.BadRequest
                );
            }

            var product = db.Products
                .Include("Category")
                .SingleOrDefault(p => p.ProductID == id.Value);

            return product == null
                ? (ActionResult)HttpNotFound()
                : View(product);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var product = db.Products.Find(id);

            if (product == null)
            {
                return HttpNotFound();
            }

            if (db.OrderDetails.Any(d => d.ProductID == id))
            {
                TempData["Error"] =
                    "Không thể xóa sản phẩm đã phát sinh trong đơn hàng. " +
                    "Hãy đặt số lượng tồn bằng 0.";

                return RedirectToAction("Index");
            }

            db.Products.Remove(product);
            db.SaveChanges();

            TempData["Success"] = "Đã xóa sản phẩm.";

            return RedirectToAction("Index");
        }

        private void LoadCategories(int? selected = null)
        {
            ViewBag.CategoryID = new SelectList(
                db.Categories.OrderBy(c => c.CategoryName),
                "CategoryID",
                "CategoryName",
                selected
            );
        }

        private bool IsImage(HttpPostedFileBase file)
        {
            var ext = Path.GetExtension(file.FileName)
                .ToLowerInvariant();

            return new[]
            {
                ".jpg",
                ".jpeg",
                ".png",
                ".gif"
            }.Contains(ext);
        }

        private string SaveImage(
            HttpPostedFileBase file,
            string oldPath)
        {
            if (file == null || file.ContentLength == 0)
            {
                return oldPath;
            }

            string folder = Server.MapPath("~/Images/Products");

            Directory.CreateDirectory(folder);

            string name = Guid.NewGuid().ToString("N") +
                Path.GetExtension(file.FileName).ToLowerInvariant();

            file.SaveAs(Path.Combine(folder, name));

            return "/Images/Products/" + name;
        }

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
