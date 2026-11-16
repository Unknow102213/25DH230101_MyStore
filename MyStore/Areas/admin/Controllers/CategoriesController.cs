using System.Data.Entity;
using MyStore.Infrastructure;
using MyStore.Models;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace MyStore.Areas.admin.Controllers
{
    [AdminAuthorize]
    public class CategoriesController : Controller
    {
        private readonly MyStoreEntities db = new MyStoreEntities();
        public ActionResult Index() 
        { 
            return View(db.Categories.Include("Products").OrderBy(c => c.CategoryName).ToList()); 
        }
        public ActionResult Details(int? id)
        {
            if (!id.HasValue) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var category = db.Categories.Include("Products").SingleOrDefault(c => c.CategoryID == id.Value);
            return category == null ? (ActionResult)HttpNotFound() : View(category);
        }
        public ActionResult Create() 
        { 
            return View(); 
        }
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Create(Category category)
        {
            if (ModelState.IsValid) 
            { 
                db.Categories.Add(category); 
                db.SaveChanges(); 
                TempData["Success"] = "Đã thêm danh mục."; 
                return RedirectToAction("Index"); 
            }
            return View(category);
        }
        public ActionResult Edit(int? id)
        {
            if (!id.HasValue) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var category = db.Categories.Find(id.Value); 
            return category == null ? (ActionResult)HttpNotFound() : View(category);
        }
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Edit(Category model)
        {
            if (!ModelState.IsValid) return View(model);
            var category = db.Categories.Find(model.CategoryID); 
            if (category == null) return HttpNotFound();
            category.CategoryName = model.CategoryName.Trim(); 
            db.SaveChanges(); 
            TempData["Success"] = "Đã cập nhật danh mục."; 
            return RedirectToAction("Index");
        }
        public ActionResult Delete(int? id)
        {
            if (!id.HasValue) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var category = db.Categories.Find(id.Value); 
            return category == null ? (ActionResult)HttpNotFound() : View(category);
        }
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var category = db.Categories.Find(id); 
            if (category == null) return HttpNotFound();
            if (db.Products.Any(p => p.CategoryID == id)) 
            { 
                TempData["Error"] = "Không thể xóa danh mục đang có sản phẩm."; 
                return RedirectToAction("Index"); 
            }
            db.Categories.Remove(category); 
            db.SaveChanges(); 
            TempData["Success"] = "Đã xóa danh mục."; 
            return RedirectToAction("Index");
        }
        protected override void Dispose(bool disposing) 
        { 
            if (disposing) db.Dispose(); 
            base.Dispose(disposing); 
        }
    }
}
