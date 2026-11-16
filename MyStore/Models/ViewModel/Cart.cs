using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MyStore.Models.ViewModel
{
    public class Cart
    {
        private List<CartItem> items = new List<CartItem>();
        public IEnumerable<CartItem> Items => items;        
        public int pageNumber { get; set; } // Trang hiện tại
        public int PageSize { get; set; } = 6; // Số sản phẩm mỗi trang mặc định là 6
        public PagedList.IPagedList<Product> SimilarProducts { get; set; } // Danh sách sản phẩm tương tự được phân trang
        public void AddItem(int productId, string productImage, string productName, decimal unitPrice, int quantity, string category, int stockQuantity = int.MaxValue)
        {
            var existingItem = items.FirstOrDefault(i => i.ProductID == productId);
            if (existingItem == null) items.Add(new CartItem { ProductID = productId, ProductImage = productImage, ProductName = productName, UnitPrice = unitPrice, Quantity = quantity, Category = category, StockQuantity = stockQuantity });
            else { existingItem.Quantity += quantity; existingItem.StockQuantity = stockQuantity; }
        }
        public void RemoveItem(int productId) => items.RemoveAll(i => i.ProductID == productId);
        public decimal TotalValue() => items.Sum(i => i.TotalPrice);
        public void Clear() => items.Clear();
        public void UpdateQuantity(int productId, int quantity)
        {
            var item = items.FirstOrDefault(i => i.ProductID == productId);
            if (item != null) item.Quantity = quantity;
        }
        public List<IGrouping<string, CartItem>> GroupedItems => items.GroupBy(i => i.Category).ToList();
    }
}