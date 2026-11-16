using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyStore.Models
{
    [MetadataType(typeof(CategoryMetadata))]
    public partial class Category { }
    public class CategoryMetadata
    {
        [Required(ErrorMessage = "Vui lòng nhập tên danh mục")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Tên danh mục từ 2 đến 100 ký tự")]
        [Display(Name = "Tên danh mục")]
        public string CategoryName { get; set; }
    }

    [MetadataType(typeof(ProductMetadata))]
    public partial class Product { }
    public class ProductMetadata
    {
        [Required, StringLength(150, MinimumLength = 2)]
        [Display(Name = "Tên sản phẩm")]
        public string ProductName { get; set; }
        [Required, Range(typeof(decimal), "0.01", "999999999", ErrorMessage = "Giá phải lớn hơn 0")]
        [Display(Name = "Đơn giá")]
        public decimal ProductPrice { get; set; }
        [Required, StringLength(4000, MinimumLength = 5)]
        [Display(Name = "Mô tả")]
        public string ProductDescription { get; set; }
        [Display(Name = "Danh mục"), Required]
        public int CategoryID { get; set; }
        [Range(0, 1000000)]
        [Display(Name = "Số lượng tồn")]
        public int Quantity { get; set; }
    }

    [MetadataType(typeof(UserMetadata))]
    public partial class User { }
    public class UserMetadata
    {
        [Required, StringLength(50, MinimumLength = 3)]
        public string Username { get; set; }
        [Required, StringLength(255, MinimumLength = 8)]
        public string Password { get; set; }
        [Required, StringLength(1)]
        public string UserRole { get; set; }
    }

    [MetadataType(typeof(CustomerMetadata))]
    public partial class Customer { }
    public class CustomerMetadata
    {
        [Required, StringLength(100)] public string CustomerName { get; set; }
        [Required, Phone, StringLength(15)] public string CustomerPhone { get; set; }
        [EmailAddress, StringLength(255)] public string CustomerEmail { get; set; }
        [StringLength(500)] public string CustomerAddress { get; set; }
    }

    [MetadataType(typeof(OrderMetadata))]
    public partial class Order { }
    public class OrderMetadata
    {
        [Required, StringLength(500)] public string ShippingAddress { get; set; }
        [Required, StringLength(50)] public string OrderStatus { get; set; }
    }

    public partial class OrderDetail
    {
        [NotMapped]
        public decimal TotalPrice { get { return Quantity * UnitPrice; } }
    }
}
