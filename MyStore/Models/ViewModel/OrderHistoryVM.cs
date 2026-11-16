using System.Collections.Generic;
using MyStore.Models;

namespace MyStore.Models.ViewModel
{
    public class OrderHistoryGroupVM
    {
        public string Status { get; set; }
        public List<Order> Orders { get; set; }
        public int TotalCount { get; set; }
    }
    public class OrderHistoryVM
    {
        public string SearchTerm { get; set; }
        public string Status { get; set; }
        public bool ShowMore { get; set; }
        public bool HasAnyOrders { get; set; }
        public List<OrderHistoryGroupVM> Groups { get; set; }
    }
    public class AdminOrderSearchVM
    {
        public string CustomerName { get; set; }
        public string PaymentStatus { get; set; }
        public System.DateTime? OrderDate { get; set; }
        public PagedList.IPagedList<Order> Orders { get; set; }
    }
}
