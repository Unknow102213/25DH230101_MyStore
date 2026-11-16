
using System.Linq;
using System.Net;
using System.Web.Mvc;

using MyStore.Infrastructure;
using MyStore.Models;
using MyStore.Models.Security;

namespace MyStore.Areas.admin.Controllers
{
    [AdminAuthorize]
    public class UsersController : Controller
    {
        private readonly MyStoreEntities db = new MyStoreEntities();

        public ActionResult Index()
        {
            return View(
                db.Users
                    .Where(u => u.UserRole == "A")
                    .OrderBy(u => u.Username)
                    .ToList()
            );
        }

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Create(string Username, string Password)
        {
            if (string.IsNullOrWhiteSpace(Username) ||
                Username.Trim().Length < 3)
            {
                ModelState.AddModelError(
                    "Username",
                    "Tên đăng nhập tối thiểu 3 ký tự."
                );
            }

            if (string.IsNullOrWhiteSpace(Password) ||
                Password.Length < 8)
            {
                ModelState.AddModelError(
                    "Password",
                    "Mật khẩu tối thiểu 8 ký tự."
                );
            }

            if (!string.IsNullOrWhiteSpace(Username) &&
                db.Users.Any(u => u.Username == Username.Trim()))
            {
                ModelState.AddModelError(
                    "Username",
                    "Tên đăng nhập đã tồn tại."
                );
            }

            if (!ModelState.IsValid)
            {
                return View();
            }

            db.Users.Add(new User
            {
                Username = Username.Trim(),
                Password = PasswordHasher.Hash(Password),
                UserRole = "A"
            });

            db.SaveChanges();

            return RedirectToAction("Index");
        }

        public ActionResult Edit(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(
                    HttpStatusCode.BadRequest
                );
            }

            var user = db.Users
                .SingleOrDefault(u =>
                    u.Username == id &&
                    u.UserRole == "A"
                );

            return user == null
                ? (ActionResult)HttpNotFound()
                : View(user);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Edit(string Username, string Password)
        {
            var user = db.Users
                .SingleOrDefault(u =>
                    u.Username == Username &&
                    u.UserRole == "A"
                );

            if (user == null)
            {
                return HttpNotFound();
            }

            if (!string.IsNullOrWhiteSpace(Password))
            {
                if (Password.Length < 8)
                {
                    ModelState.AddModelError(
                        "Password",
                        "Mật khẩu tối thiểu 8 ký tự."
                    );
                }
                else
                {
                    user.Password = PasswordHasher.Hash(Password);
                }
            }

            if (!ModelState.IsValid)
            {
                return View(user);
            }

            db.SaveChanges();

            return RedirectToAction("Index");
        }

        public ActionResult Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(
                    HttpStatusCode.BadRequest
                );
            }

            var user = db.Users
                .SingleOrDefault(u =>
                    u.Username == id &&
                    u.UserRole == "A"
                );

            return user == null
                ? (ActionResult)HttpNotFound()
                : View(user);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string id)
        {
            if (id == User.Identity.Name)
            {
                TempData["Error"] =
                    "Không thể xóa tài khoản đang đăng nhập.";

                return RedirectToAction("Index");
            }

            var user = db.Users
                .SingleOrDefault(u =>
                    u.Username == id &&
                    u.UserRole == "A"
                );

            if (user == null)
            {
                return HttpNotFound();
            }

            if (db.Customers.Any(c => c.Username == id))
            {
                TempData["Error"] =
                    "Tài khoản đang liên kết khách hàng, không thể xóa.";

                return RedirectToAction("Index");
            }

            db.Users.Remove(user);
            db.SaveChanges();

            return RedirectToAction("Index");
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
