using MyStore.Models;
using MyStore.Models.Security;
using MyStore.Models.ViewModel;
using System.Linq;
using System.Web.Mvc;
using System.Web.Security;

namespace MyStore.Controllers
{
    public class AccountController : Controller
    {
        private readonly MyStoreEntities db = new MyStoreEntities();

        [AllowAnonymous]
        public ActionResult Register() { return View(); }

        [HttpPost, ValidateAntiForgeryToken, AllowAnonymous]
        public ActionResult Register(RegisterVM model)
        {
            if (!ModelState.IsValid) return View(model);
            if (db.Users.Any(u => u.Username == model.Username))
            {
                ModelState.AddModelError("Username", "Tên đăng nhập đã tồn tại.");
                return View(model);
            }
            if (!string.IsNullOrWhiteSpace(model.CustomerEmail) && db.Customers.Any(c => c.CustomerEmail == model.CustomerEmail))
            {
                ModelState.AddModelError("CustomerEmail", "Email đã được sử dụng.");
                return View(model);
            }
            if (!string.IsNullOrWhiteSpace(model.CustomerPhone) && db.Customers.Any(c => c.CustomerPhone == model.CustomerPhone))
            {
                ModelState.AddModelError("CustomerPhone", "Số điện thoại đã được sử dụng.");
                return View(model);
            }
            using (var tx = db.Database.BeginTransaction())
            {
                var user = new User { Username = model.Username.Trim(), Password = PasswordHasher.Hash(model.Password), UserRole = "C" };
                db.Users.Add(user);
                db.SaveChanges();
                db.Customers.Add(new Customer { Username = user.Username, CustomerName = model.CustomerName.Trim(), CustomerPhone = model.CustomerPhone.Trim(), CustomerEmail = model.CustomerEmail, CustomerAddress = model.CustomerAddress });
                db.SaveChanges();
                tx.Commit();
            }
            TempData["Success"] = "Đăng ký thành công. Vui lòng đăng nhập.";
            return RedirectToAction("Login");
        }

        [AllowAnonymous]
        public ActionResult Login() { return View(); }

        [HttpPost, ValidateAntiForgeryToken, AllowAnonymous]
        public ActionResult Login(LoginVM model)
        {
            if (!ModelState.IsValid) return View(model);
            var user = db.Users.SingleOrDefault(u => u.Username == model.Username);
            if (user == null || !PasswordHasher.Verify(model.Password, user.Password))
            {
                ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không đúng.");
                return View(model);
            }
            // Chuyển mật khẩu cũ dạng rõ sang PBKDF2 sau lần đăng nhập hợp lệ.
            if (!user.Password.StartsWith("PBKDF2$"))
            {
                user.Password = PasswordHasher.Hash(model.Password);
                db.SaveChanges();
            }
            Session["Username"] = user.Username;
            Session["UserRole"] = user.UserRole;
            FormsAuthentication.SetAuthCookie(user.Username, false);
            if (user.UserRole == "A") return RedirectToAction("Index", "Home", new { area = "admin" });
            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        public ActionResult Profile()
        {
            var customer = db.Customers.SingleOrDefault(c => c.Username == User.Identity.Name);
            if (customer == null) return HttpNotFound();
            return View(customer);
        }

        public ActionResult Logout()
        {
            Session.Clear();
            FormsAuthentication.SignOut();
            return RedirectToAction("Index", "Home");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
