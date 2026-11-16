using MyStore.Models;
using PagedList;
using System;
using System.Collections.Generic;

namespace MyStore.Models.ViewModel
{
    public class ProductDetailsVM
    {
        public Product product { get; set; }
        public int quantity { get; set; } = 1;
        public decimal estimatedValue { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; } = 3;
        public IPagedList<Product> RelatedProducts { get; set; }
        public PagedList.IPagedList<Product> TopProducts { get; set; }
    }
}