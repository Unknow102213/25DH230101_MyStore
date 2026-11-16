using System.ComponentModel.DataAnnotations;
namespace MyStore.Models.ViewModel
{
    public class RegisterVM
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập"), StringLength(50, MinimumLength = 3)] [Display(Name = "Tên đăng nhập")] public string Username { get; set; }
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu"), StringLength(100, MinimumLength = 8)] [DataType(DataType.Password), Display(Name = "Mật khẩu")] public string Password { get; set; }
        [Required, DataType(DataType.Password), Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp")] [Display(Name = "Xác nhận mật khẩu")] public string ConfirmPassword { get; set; }
        [Required, StringLength(100), Display(Name = "Họ tên")] public string CustomerName { get; set; }
        [Required, Phone, StringLength(15), Display(Name = "Số điện thoại")] public string CustomerPhone { get; set; }
        [Required, EmailAddress, StringLength(255), Display(Name = "Email")] public string CustomerEmail { get; set; }
        [Required, StringLength(500), Display(Name = "Địa chỉ")] public string CustomerAddress { get; set; }
    }
}
